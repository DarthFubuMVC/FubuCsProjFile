﻿using System;
using System.Collections.Generic;
using FubuCore;

namespace FubuCsProjFile.Templating
{
    public abstract class TemplatePlanner : ITemplatePlannerAction
    {
        private readonly IList<ITemplatePlanner> _planners = new List<ITemplatePlanner>();
        private FileSet _matching;

        protected TemplatePlanner()
        {
            ShallowMatch(GemReference.File).Do = GemReference.ConfigurePlan;
            ShallowMatch(GitIgnoreStep.File).Do = GitIgnoreStep.ConfigurePlan;

            // TODO -- add the rake transform
        }

        public void CreatePlan(string directory, TemplatePlan plan)
        {
            configurePlan(directory, plan);

            _planners.Each(x => x.DetermineSteps(directory, plan));

            plan.CopyUnhandledFiles(directory);
        }

        protected abstract void configurePlan(string directory, TemplatePlan plan);

        public void Add<T>() where T : ITemplatePlanner, new()
        {
            _planners.Add(new T());
        }

        public ITemplatePlannerAction Matching(FileSet matching)
        {
            _matching = matching;
            return this;
        }

        public ITemplatePlannerAction DeepMatch(string pattern)
        {
            return Matching(FileSet.Deep(pattern));
        }

        public ITemplatePlannerAction ShallowMatch(string pattern)
        {
            return Matching(FileSet.Shallow(pattern));
        }

        public Action<TextFile, TemplatePlan> Do
        {
            set 
            { 
                var planner = new FilesTemplatePlanner(_matching, value);
                _planners.Add(planner);
            }
        }
    }
}