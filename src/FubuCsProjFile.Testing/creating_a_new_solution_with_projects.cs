﻿using System;
using System.Diagnostics;
using System.IO;
using FubuCore;
using FubuCsProjFile.Templating;
using FubuTestingSupport;
using NUnit.Framework;
using System.Linq;

namespace FubuCsProjFile.Testing
{

    [TestFixture]
    public class creating_a_new_solution_with_projects
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            var fileSystem = new FileSystem();
            fileSystem.DeleteDirectory("TestSolution");
            fileSystem.CreateDirectory("TestSolution");
        }

        [Test]
        public void create_solution_add_project_save_and_reload()
        {
            // Yeah, this is a big bang test.  Just go with it.

            var solution = Solution.CreateNew("TestSolution", "TestSolution");

            var reference = solution.AddProject("TestProject");
            reference.ProjectGuid.ShouldNotEqual(Guid.Empty);
            reference.ProjectName.ShouldEqual("TestProject");
            reference.RelativePath.ShouldEqual("TestProject".AppendPath("TestProject.csproj"));

            CodeFileTemplate.Class("Foo").Attach(reference.Project);
            CodeFileTemplate.Class("Bar").Attach(reference.Project);

            solution.Save();

            File.Exists("TestSolution".AppendPath("TestSolution.sln")).ShouldBeTrue();
            File.Exists("TestSolution".AppendPath("TestProject", "TestProject.csproj")).ShouldBeTrue();

            var solution2 = Solution.LoadFrom("TestSolution".AppendPath("TestSolution.sln"));
            var reference2 = solution2.FindProject("TestProject");

            reference2.ShouldNotBeNull();

            var project2 = reference2.Project;
            project2.ShouldNotBeNull();

            project2.All<CodeFile>().OrderBy(x => x.Include)
                .Select(x => x.Include)
                .ShouldHaveTheSameElementsAs("Bar.cs", "Foo.cs");
        }
    }
}