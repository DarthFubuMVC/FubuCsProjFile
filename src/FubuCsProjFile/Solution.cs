﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FubuCore;
using System.Linq;
using FubuCore.Util;
using FubuCsProjFile.MSBuild;

namespace FubuCsProjFile
{
    public class Solution
    {
        private const string Global = "Global";
        private const string EndGlobal = "EndGlobal";
        public const string EndGlobalSection = "EndGlobalSection";
        private const string SolutionConfigurationPlatforms = "SolutionConfigurationPlatforms";
        private const string ProjectConfigurationPlatforms = "ProjectConfigurationPlatforms";

        public static readonly string VS2010 = "VS2010";
        public static readonly string VS2012 = "VS2012";
        public static readonly string VS2013 = "VS2013";

        private static readonly Cache<string, string[]> _versionLines = new Cache<string, string[]>();

        static Solution()
        {
            _versionLines[VS2010] = new[] { "Microsoft Visual Studio Solution File, Format Version 11.00", "# Visual Studio 2010" };
            _versionLines[VS2012] = new[] { "Microsoft Visual Studio Solution File, Format Version 12.00", "# Visual Studio 2012" };
            _versionLines[VS2013] = new[] { "Microsoft Visual Studio Solution File, Format Version 13.00", "# Visual Studio 2013" };
        }

        private readonly string _filename;
        private readonly IList<SolutionProject> _projects = new List<SolutionProject>(); 

        public static Solution CreateNew(string directory, string name)
        {
            var text = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof (Solution), "Solution.txt")
                               .ReadAllText();

            var filename = directory.AppendPath(name);
            if (Path.GetExtension(filename) != ".sln")
            {
                filename = filename + ".sln";
            }

            return new Solution(filename, text)
            {
                Version = VS2010
            };
        }

        public static Solution LoadFrom(string filename)
        {
            var text = new FileSystem().ReadStringFromFile(filename);
            return new Solution(filename, text);
        }

        private Solution(string filename, string text)
        {
            _filename = filename;
            var items = text.SplitOnNewLine();
            var reader = new SolutionReader(this);
            items.Each(reader.Read);
        }

        private readonly IList<string> _globals = new List<string>(); 
        private readonly IList<GlobalSection> _sections = new List<GlobalSection>();

        public string Version { get; set; }

        public string Filename
        {
            get { return _filename; }
        }

        public IList<GlobalSection> Sections
        {
            get { return _sections; }
        }

        public IEnumerable<string> Globals
        {
            get { return _globals; }
        }

        public IEnumerable<BuildConfiguration> Configurations()
        {
            var section = FindSection(SolutionConfigurationPlatforms);
            return section == null
                       ? Enumerable.Empty<BuildConfiguration>()
                       : section.Properties.Select(x => new BuildConfiguration(x));
        }

        public class SolutionReader
        {
            private readonly Solution _parent;
            private Action<string> _read;
            private GlobalSection _section;
            private SolutionProject _solutionProject;

            public SolutionReader(Solution parent)
            {
                _parent = parent;

                _read = normalRead;
            }

            private void lookForGlobalSection(string text)
            {
                text = text.Trim();
                if (text.Trim().StartsWith("GlobalSection"))
                {
                    _section = new GlobalSection(text);
                    _parent._sections.Add(_section);
                    _read = readSection;
                }
            }

            private void readSection(string text)
            {
                if (text.Trim() == EndGlobalSection)
                {
                    _read = lookForGlobalSection;
                }
                else
                {
                    _section.Read(text);
                }
            }

            private void readProject(string text)
            {
                if (text.StartsWith("EndProject"))
                {
                    _read = normalRead;
                }
                else
                {
                    _solutionProject.ReadLine(text);
                }
            }

            private void normalRead(string text)
            {
                if (text.StartsWith(Global))
                {
                    _read = lookForGlobalSection;
                }
                else if (text.StartsWith("Project"))
                {
                    _solutionProject = new SolutionProject(text, _parent._filename.ParentDirectory());
                    _parent._projects.Add(_solutionProject);
                    _read = readProject;
                }
                else if (_parent.Version.IsEmpty())
                {
                    foreach (var versionLine in _versionLines.ToDictionary())
                    {
                        if (text.Trim() == versionLine.Value.First())
                        {
                            _parent.Version = versionLine.Key;
                        }
                    }
                }
            }

            public void Read(string text)
            {
                _read(text);
            }


        }

        public GlobalSection FindSection(string name)
        {
            return _sections.FirstOrDefault(x => x.SectionName == name);
        }

        public void Save()
        {
            Save(_filename);
        }

        public void Save(string filename)
        {
            calculateProjectConfigurationPlatforms();

            var writer = new StringWriter();

            _versionLines[Version].Each(x => writer.WriteLine(x));

            _projects.Each(x => x.Write(writer));

            writer.WriteLine(Global);

            _sections.Each(x => x.Write(writer));

            writer.WriteLine(EndGlobal);


            new FileSystem().WriteStringToFile(filename, writer.ToString().TrimEnd());

            _projects.Each(x => x.Project.Save());
        }

        private void calculateProjectConfigurationPlatforms()
        {
            var section = FindSection(ProjectConfigurationPlatforms);
            if (section == null)
            {
                section = new GlobalSection("GlobalSection(ProjectConfigurationPlatforms) = postSolution");
                _sections.Add(section);
            }

            section.Properties.Clear();
            var configurations = Configurations().ToArray();

            _projects.Where(x => x.ProjectName != "Solution Items").Each(proj => {
                configurations.Each(config => config.WriteProjectConfiguration(proj, section));
            });


        }

        public IEnumerable<SolutionProject> Projects
        {
            get { return _projects; }
        }

        public SolutionProject AddProject( string projectName)
        {
            var existing = FindProject(projectName);
            if (existing != null)
            {
                return existing;
            }

            var reference = SolutionProject.CreateNewAt(ParentDirectory, projectName);
            _projects.Add(reference);

            return reference;
        }

        public SolutionProject AddProjectFromTemplate(string projectName, string templateFile)
        {
            var existing = FindProject(projectName);
            if (existing != null)
            {
                throw new ArgumentOutOfRangeException("projectName", "Project with this name ({0}) already exists in the solution".ToFormat(projectName));
            }


            var project = MSBuildProject.CreateFromFile(projectName, templateFile);
            var csProjFile = new CsProjFile(ParentDirectory.AppendPath(projectName, projectName + ".csproj"), project);

            var reference = new SolutionProject(csProjFile, ParentDirectory);
            _projects.Add(reference);

            return reference;
        }

        public string ParentDirectory
        {
            get { return _filename.ParentDirectory(); }
        }

        public string Name
        {
            get { return Path.GetFileNameWithoutExtension(_filename); }
        }

        public SolutionProject FindProject(string projectName)
        {
            return _projects.FirstOrDefault(x => x.ProjectName == projectName);
        }
    }
}