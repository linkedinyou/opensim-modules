/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using log4net;
using Nini.Config;
using OpenMetaverse;
using Mono.Addins;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using IronPython.Runtime;

[assembly: Addin("XPython.Module", "1.0")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace XPython
{
    public delegate void AddRegionEvent(Scene scene);
    public delegate void RegionLoadedEvent(Scene scene);
    public delegate void RemoveRegionEvent(Scene scene);

    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "XPython")]
    public class XPythonModule : INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        Scene m_Scene;
		IConfigSource m_Config;
		
        private ScriptEngine m_pyengine = null;
        private ScriptScope m_pyscope = null;

        public event AddRegionEvent OnAddRegion;
        public event RegionLoadedEvent OnRegionLoaded;
        public event RemoveRegionEvent OnRemoveRegion;
		
        #region IRegionModule interface

        public void Initialise(IConfigSource config)
        {
            m_log.Info("[PythonModuleLoader] Initializing...");
			m_Config = config;
            m_pyengine = Python.CreateEngine();
            m_pyscope = m_pyengine.CreateScope();
            ICollection<string> paths = m_pyengine.GetSearchPaths();
            paths.Add(AppDomain.CurrentDomain.BaseDirectory);
			m_pyengine.SetSearchPaths(paths);
			m_log.Info("Added " + AppDomain.CurrentDomain.BaseDirectory +
                    " to python module search path");

            ScriptSource source = null;
			m_pyscope.SetVariable("config", m_Config);
			m_pyscope.SetVariable("module", this);
            source = m_pyengine.CreateScriptSourceFromString(
                    "try:\n" +
                    "  import pymodloader\n" +
                    "  pymodloader.init(module, config)\n" +
                    "except Exception, e:\n" +
                    "  import traceback\n" +
                    "  traceback.print_exc()\n",
                     SourceCodeKind.Statements);
            source.Execute(m_pyscope);
        }

        public void AddRegion(Scene scene)
        {
            m_Scene = scene;

            AddRegionEvent e = OnAddRegion;
            if (e != null)
                e(scene);
        }

        public void RegionLoaded(Scene scene)
        {
            m_Scene = scene;

            RegionLoadedEvent e = OnRegionLoaded;
            if (e != null)
                e(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            m_Scene = scene;

            RemoveRegionEvent e = OnRemoveRegion;
            if (e != null)
                e(scene);
        }

		public void Close()
        {
        }

        public string Name
        {
            get { return "Python module loader"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

    }
}