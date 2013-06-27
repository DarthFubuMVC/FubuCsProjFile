﻿using System.Collections.Generic;

namespace FubuCsProjFile.Templating
{
    public class SolutionPlanner : TemplatePlanner
    {
        public SolutionPlanner()
        {
            ShallowMatch(Input.File).Do = (file, plan) => {
                var inputs = Input.ReadFromFile(file.Path);
                plan.Substitutions.ReadInputs(inputs);
            };
        }

        protected override void configurePlan(string directory, TemplatePlan plan)
        {
            SolutionDirectory.PlanForDirectory(directory).Each(plan.Add);
        }
    }
}