﻿using System;
using System.Diagnostics;
using System.IO;
using FubuCore;
using FubuCsProjFile.ProjectFiles.CsProj;
using FubuCsProjFile.SolutionFile;
using NUnit.Framework;
using FubuTestingSupport;
using System.Collections.Generic;
using System.Linq;

namespace FubuCsProjFile.Testing
{
    [TestFixture]
    public class SolutionTester
    {
        [Test]
        public void default_version_is_VS2010_for_now()
        {
            SolutionBuilder.CreateNew("foo", "Foo")
                .Version.ShouldEqual(Solution.VS2010);
        }

        [Test]
        public void create_new_and_read_build_configurations()
        {
            var solution = SolutionBuilder.CreateNew(".", "foo");
            solution.Configurations().ShouldHaveTheSameElementsAs(
                new BuildConfiguration("Debug|Any CPU = Debug|Any CPU"),
                new BuildConfiguration("Debug|x86 = Debug|x86"),
                new BuildConfiguration("Release|Any CPU = Release|Any CPU"),
                new BuildConfiguration("Release|x86 = Release|x86")
            );
        }

        [Test]
        public void create_new_and_read_other_globals()
        {
            var solution = SolutionBuilder.CreateNew(".", "foo");
            solution.FindSection("SolutionProperties").Properties
                .ShouldHaveTheSameElementsAs("HideSolutionNode = FALSE");
        }

        [Test]
        public void write_a_solution()
        {
            var solution = SolutionBuilder.CreateNew(".".ToFullPath(), "foo");
            solution.Save();

            var original =
                new FileSystem().ReadStringFromFile(
                    ".".ToFullPath()
                       .ParentDirectory()
                       .ParentDirectory()
                       .ParentDirectory()
                       .AppendPath("FubuCsProjFile", "Solution.txt")).SplitOnNewLine();



            var newContent = new FileSystem().ReadStringFromFile("foo.sln").SplitOnNewLine();

            // skipping 2 is to ignore the version content
            newContent.Skip(2).ShouldHaveTheSameElementsAs(original);

        }

        [Test]
        public void read_a_solution_with_projects()
        {
            
            var solution = SolutionReader.LoadFrom("FubuMVC.SlickGrid.sln");
            solution.Projects.Select(x => x.ProjectName)
                .ShouldHaveTheSameElementsAs("Solution Items", "FubuMVC.SlickGrid", "FubuMVC.SlickGrid.Testing", "SlickGridHarness", "FubuMVC.SlickGrid.Serenity", "FubuMVC.SlickGrid.Docs");
        }

        [Test]
        public void read_and_write_a_solution_with_projects()
        {
            // SAMPLE: Loading-and-Saving
            var solution = SolutionReader.LoadFrom("FubuMVC.SlickGrid.sln");
            solution.Save("fake.sln");
            // ENDSAMPLE

            var original =
                new FileSystem().ReadStringFromFile("FubuMVC.SlickGrid.sln").Trim().SplitOnNewLine()
                .Select(x => x.Replace('\\', '/'));

            var newContent = new FileSystem().ReadStringFromFile("fake.sln").SplitOnNewLine().Select(x => x.Replace('\\', '/'));

            newContent.Each(x => Debug.WriteLine(x));

            newContent.ShouldHaveTheSameElementsAs(original);
        }

        [Test]
        public void adding_a_project_is_idempotent()
        {
            var solution = SolutionReader.LoadFrom("FubuMVC.SlickGrid.sln");
            var projectName = solution.Projects.Last().ProjectName;

            var initialCount = solution.Projects.Count();

            solution.GetOrAddProject(projectName);
            solution.GetOrAddProject(projectName);
            solution.GetOrAddProject(projectName);
            solution.GetOrAddProject(projectName);

            solution.Projects.Count().ShouldEqual(initialCount);

        }

        [Test]
        public void add_a_project_from_template()
        {
            // SAMPLE: create-project-by-template
            var solution = SolutionReader.LoadFrom("FubuMVC.SlickGrid.sln");
            var reference = solution.AddProjectFromTemplate("MyNewProject", Path.Combine("..", "..", "Project.txt"));
            // ENDSAMPLE

            reference.Project.Find<AssemblyReference>("System.Data")
                     .ShouldNotBeNull();

            solution.Save("foo.sln");

            // saves to the right spot
            File.Exists("MyNewProject".AppendPath("MyNewProject.csproj"))
                .ShouldBeTrue();
        }

        [Test]
        public void add_an_existing_project_to_a_new_solution()
        {
            var solution = SolutionBuilder.CreateNew(@"solutions\sillydir", "newsolution");
            File.Copy("FubuMVC.SlickGrid.Docs.csproj.fake", "FubuMVC.SlickGrid.Docs.csproj", true);

            solution.AddProject(CsProjFile.LoadFrom("FubuMVC.SlickGrid.Docs.csproj"));

            solution.FindProject("FubuMVC.SlickGrid.Docs").ShouldNotBeNull();
        }

        [Test]
        public void trying_to_add_a_project_from_template_that_already_exists_should_throw()
        {
            var solution = SolutionReader.LoadFrom("FubuMVC.SlickGrid.sln");
            var projectName = solution.Projects.OfType<ISolutionProjectFile>().First().ProjectName;

            Exception<ArgumentOutOfRangeException>.ShouldBeThrownBy(() => {
                solution.AddProjectFromTemplate(projectName, Path.Combine("..", "..", "Project.txt"));
            });
        }
    }
}