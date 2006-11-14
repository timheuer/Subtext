#region Disclaimer/Info
///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at SourceForge at http://sourceforge.net/projects/subtext
// The development mailing list is at subtext-devs@lists.sourceforge.net 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Configuration;
using System.Web.Configuration;
using Subtext.Framework.Configuration;
using Subtext.Framework.Components;
using Subtext.Framework.Logging;

namespace Subtext.Extensibility.Plugins
{

	/// <summary>
	/// Singleton class.<br/>
	/// This class is responsible for all the actions related to plugins:<br/>
	/// <ul>
	/// <list type=">">Loading settings from web.config</list>
	/// <list type=">">Loading and initializing plugins from the settings loaded before</list>
	/// <list type=">">Defines all the events for the application</list>
	/// <list type=">">Manages the execution of the plugins, based on their abilitation for the current blog</list>
	/// </ul>
	/// Since the class contains a lot of event definitions, it uses the approach used also by the Form class and by HttpApplication class:<br/>
	/// uses only one EventHandlerList to store all the event subscription instead of one per event as the default behaviour
	/// </summary>
	public sealed class SubtextApplication
	{

		private EventHandlerList Events = new EventHandlerList();
		private static readonly object sync = new object();


		#region Event Keys (static)
		//Using objects so that no cast is performed when accessing the eventhandler list
		private static object EventEntryUpdating = new object();
		private static object EventEntryUpdated = new object();
		private static object EventEntryRendering = new object();
		private static object EventSingleEntryRendering = new object();
		#endregion


		private static bool _initialized = false;
		private Dictionary<Guid, PluginBase> _plugins = new Dictionary<Guid, PluginBase>();

		private static readonly Log __log = new Log();
		private static SubtextApplication _instance = LoadPlugins();

		private PluginSettingsCollection _pluginConfig;

		/// <summary>
		/// List of all available plugins configurations
		/// </summary>
		public PluginSettingsCollection PluginConfig
		{
			get { return _pluginConfig; }
		}

		/// <summary>
		/// All avalialbe plugins. These plugins are already initialized
		/// </summary>
		public Dictionary<Guid, PluginBase> Plugins
		{
			get { return _plugins; }
		}


		

		/// <summary>
		/// Returns the current single instance of the class
		/// </summary>
		public static SubtextApplication Current
		{
			get
			{
				//In case the static initializer has failed
				//I try to reinitialize it again
				if(!_initialized)
					_instance = LoadPlugins();
				return _instance;
			}
		}

		private SubtextApplication()
		{
		}

		/// <summary>
		/// Load all plugins from the web.config file, and return a configured instance of the SubtextApplication, with all the plugins already initialized
		/// </summary>
		/// <returns>Configured instance of the SubtextApplication</returns>
		private static SubtextApplication LoadPlugins()
		{
			SubtextApplication app = new SubtextApplication();
			PluginSectionHandler pluginSection = (PluginSectionHandler)WebConfigurationManager.GetSection("STPluginConfiguration");

			if(pluginSection!=null) 
			{
				app._pluginConfig = pluginSection.PluginList;

				if (app._pluginConfig != null)
				{

					foreach (PluginSettings setting in app._pluginConfig)
					{

						if (String.IsNullOrEmpty(setting.Type))
						{
							__log.Warn("Cannot load plugin defined at line " + setting.ElementInformation.LineNumber + ":\r\nMissing Type");
							continue;
						}

						if (String.IsNullOrEmpty(setting.Name))
						{
							__log.Warn("Cannot load plugin defined at line " + setting.ElementInformation.LineNumber + ":\r\nMissing Name");
							continue;
						}

						Type type = Type.GetType(setting.Type);

						if (type == null)
						{
							__log.Warn("Cannot load plugin defined at line " + setting.ElementInformation.LineNumber + ":\r\nType " + setting.Type + " not found in any of the assembly available inside the \\bin folder");
							continue;
						}
						PluginBase plugin = Activator.CreateInstance(type) as PluginBase;

						if (plugin == null)
						{
							__log.Warn("Cannot load plugin defined at line " + setting.ElementInformation.LineNumber + ":\r\nType " + setting.Type + " doesn't inherits from PluginBase abstract class");
							continue;
						}

						if (plugin.Id == Guid.Empty)
						{
							__log.Warn("Cannot load plugin with name " + setting.Name + ": doesn't provide a Guid");
							continue;
						}

						if (plugin.Info == null)
						{
							__log.Warn("Cannot load plugin with name " + setting.Name + ": doesn't provide at least one descriptive metadata");
							continue;
						}

						try
						{
							plugin.Init(app);
							app._plugins.Add(plugin.Id, plugin);
						}
						catch (Exception ex)
						{
							__log.Error("Error initializing plugin with name " + setting.Name + " from type " + setting.Type + "\r\nThe Init method threw the following exception:\r\n" + ex.Message);
							continue;
						}
						
						#if DEBUG
						__log.Debug("Loaded plugin with name " + setting.Name + " from type " + setting.Type);
						#endif
						
					}
				}
			}
			_initialized = true;
			return app;
		}

		#region Event definitions

		/// <summary>
		/// Raised before changes to the Entry are committed to the datastore
		/// </summary>
		public event EventHandler<SubtextEventArgs> EntryUpdating
		{
			add
			{
				Events.AddHandler(EventEntryUpdating, value);
			}
			remove
			{
				Events.RemoveHandler(EventEntryUpdating, value);
			}
		}

		/// <summary>
		/// Raised after the changes has been committed to the datastore
		/// </summary>
		public event EventHandler<SubtextEventArgs> EntryUpdated
		{
			add
			{
				Events.AddHandler(EventEntryUpdated, value);
			}
			remove
			{
				Events.RemoveHandler(EventEntryUpdated, value);
			}
		}

		/// <summary>
		/// Raised an individual entry is rendered
		/// </summary>
		public event EventHandler<SubtextEventArgs> SingleEntryRendering
		{
			add
			{
				Events.AddHandler(EventSingleEntryRendering, value);
			}
			remove
			{
				Events.RemoveHandler(EventSingleEntryRendering, value);
			}
		}

		/// <summary>
		/// Raised when entry is rendered in the homepage
		/// </summary>
		public event EventHandler<SubtextEventArgs> EntryRendering
		{
			add
			{
				Events.AddHandler(EventEntryRendering, value);
			}
			remove
			{
				Events.RemoveHandler(EventEntryRendering, value);
			}
		}

		#endregion

		#region Event Execution

		internal void ExecuteEntryUpdating(Entry entry, SubtextEventArgs e)
		{
			ExecuteEntryEvent(EventEntryUpdating, entry, e);
		}

		internal void ExecuteEntryUpdated(Entry entry, SubtextEventArgs e)
		{
			ExecuteEntryEvent(EventEntryUpdated, entry, e);
		}

		internal void ExecuteEntryRendering(Entry entry, SubtextEventArgs e)
		{
			ExecuteEntryEvent(EventEntryRendering, entry, e);
		}

		internal void ExecuteSingleEntryRendering(Entry entry, SubtextEventArgs e)
		{
			ExecuteEntryEvent(EventSingleEntryRendering, entry, e);
		}

		//List through the subscribed event handlers, and decide weather call them or not
		//based on the current blog enabled plugins
		private void ExecuteEntryEvent(object eventKey, Entry entry, SubtextEventArgs e)
		{
			EventHandler<SubtextEventArgs> handler = Events[eventKey] as EventHandler<SubtextEventArgs>;
			if (handler != null)
			{
				Delegate[] delegates = handler.GetInvocationList();
				foreach (Delegate del in delegates)
				{
					PluginBase currentPlugin = GetPluginByType(del.Method.DeclaringType);
					if (PluginEnabled(currentPlugin))
					{
						try
						{
							e.CallingPluginGuid = currentPlugin.Id;
							del.DynamicInvoke(entry, e);
						}
						catch {}
					}
				}
			}
		}

		#endregion


		#region Helper Functions

		/// <summary>
		/// Get the initialized plugin given its guid in string format
		/// </summary>
		/// <param name="guid">the GUID in string format</param>
		/// <returns>The initialized instance of the plugin</returns>
		public PluginBase GetPluginByGuid(string guid)
		{
			return GetPluginByGuid(new Guid(guid));
		}

		/// <summary>
		/// Get the initialized plugin given its guid
		/// </summary>
		/// <param name="guid">the GUID</param>
		/// <returns>The initialized instance of the plugin</returns>
		public PluginBase GetPluginByGuid(Guid guid)
		{
			if (_plugins.ContainsKey(guid))
				return _plugins[guid];
			else
				return null;
		}


		/// <summary>
		/// Get the initialized plugin given its type
		/// </summary>
		/// <param name="type">the type of the plugin</param>
		/// <returns>The initialized instance of the plugin</returns>
		private PluginBase GetPluginByType(Type type)
		{
			foreach (PluginBase plugin in _plugins.Values)
			{
				if (plugin.GetType() == type)
					return plugin;
			}
			return null;
		}

		/// <summary>
		/// Get the file name of a plugin module given the plugin name and the module key
		/// </summary>
		/// <param name="pluginName">Plugin name</param>
		/// <param name="moduleName">Module key</param>
		/// <returns>Filename of the module</returns>
		public string GetPluginModuleFileName(string pluginName, string moduleName)
		{
			foreach (PluginSettings settings in _pluginConfig)
			{
				if (settings.Name.Equals(pluginName))
					foreach (PluginModule module in settings.Modules)
					{
						if (module.Key.Equals(moduleName))
							return module.FileName;
					}
			}
			return null;
		}

		/// <summary>
		/// Checks if the plugin is enabled for the current blog
		/// </summary>
		/// <param name="plugin">The plugin</param>
		/// <returns><c>true</c> if the plugin is enabled, <c>false</c> otherwise</returns>
		public bool PluginEnabled(PluginBase plugin)
		{
			foreach (Plugin blogPlugin in Config.CurrentBlog.EnabledPlugins.Values)
			{
				if (blogPlugin.InitializedPlugin == plugin)
					return true;
			}

			return false;
		}

		#endregion
	}


}