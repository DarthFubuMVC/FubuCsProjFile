﻿using FubuCore;
using FubuCsProjFile.Templating;
using FubuCsProjFile.Templating.Graph;
using FubuCsProjFile.Templating.Planning;
using FubuCsProjFile.Templating.Runtime;

namespace FubuCsProjFile.Testing
{
    public class DataMother
    {
        private readonly string _directory;
        private readonly TemplatePlan _plan;

        public DataMother(string directory, bool withProject = true)
        {
            _directory = directory;

            _plan = TemplatePlan.CreateClean(_directory);
            if (withProject)
            {
                _plan.Add(new ProjectPlan("SomeProject"));
            }
        }

        public FileContents ToPath(params string[] pathParts)
        {
            var path = _directory.AppendPath(pathParts);
            return new FileContents(path);
        }

        public TemplatePlan Plan
        {
            get { return _plan; }
        }

        public class FileContents
        {
            private readonly string _path;

            public FileContents(string path)
            {
                _path = path;
            }

            public void WriteContent(string content)
            {
                new FileSystem().WriteStringToFile(_path, content.TrimStart());
            }

            public void WriteEmpty()
            {
                WriteContent(string.Empty);
            }
        }

        public TemplatePlan BuildSolutionPlan()
        {
            new SolutionPlanner().CreatePlan(new Template{Path = _directory}, _plan);

            return _plan;
        }

        public TemplatePlan RunPlanner<T>() where T : TemplatePlanner, new()
        {
            new T().CreatePlan(new Template { Path = _directory }, _plan);

            return _plan;
        }
    }

    
}