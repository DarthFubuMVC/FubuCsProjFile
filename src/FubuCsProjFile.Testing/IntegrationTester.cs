﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FubuCsProjFile.Templating;
using NUnit.Framework;
using FubuCore;
using FubuTestingSupport;
using System.Linq;

namespace FubuCsProjFile.Testing
{
    [TestFixture]
    public class IntegrationTester
    {
        private TemplateLibrary library;
        private TemplatePlan thePlan;
        private TemplateRequest theRequest;

        [SetUp]
        public void SetUp()
        {
            library =
                new TemplateLibrary(
                    ".".ToFullPath().ParentDirectory().ParentDirectory().ParentDirectory().AppendPath("templates"));

            theRequest = new TemplateRequest
            {
                RootDirectory = "integrated",
                SolutionName = "MySolution"
            };

            new FileSystem().DeleteDirectory("integrated");
        }

        private void executePlan(Action<TemplateRequest> configure = null)
        {
            if (configure != null)
            {
                configure(theRequest);
            }

            thePlan = new TemplatePlanBuilder(library).BuildPlan(theRequest);

            thePlan.Execute();
        }   

        public void assertDirectoryExists(params string[] parts)
        {
            Directory.Exists("integrated".AppendPath(parts)).ShouldBeTrue();
        }

        private void assertFileExists(params string[] parts)
        {
            File.Exists("integrated".AppendPath(parts)).ShouldBeTrue();
        }

        private IList<string> readFile(params string[] parts)
        {
            assertFileExists(parts);

            return new FileSystem().ReadStringFromFile("integrated".AppendPath(parts)).ReadLines().ToList();
        }

        [Test]
        public void solution_folders()
        {
            executePlan(x => x.AddTemplate("Simple"));

            thePlan.Steps.OfType<SolutionDirectory>().Any().ShouldBeTrue();

            assertDirectoryExists("src");
            assertDirectoryExists("packaging");
            assertDirectoryExists("packaging", "nuget");
        }

        [Test]
        public void solution_files()
        {
            executePlan(x => x.AddTemplate("Simple"));

            thePlan.Steps.OfType<CopyFileToSolution>().Any().ShouldBeTrue();

            assertFileExists("something.txt");
            assertFileExists("else.txt");
            assertFileExists("packaging", "nuget", "nuspec.txt");
        }

        [Test]
        public void rake_files()
        {
            executePlan(x => x.AddTemplate("Simple"));
            thePlan.Steps.OfType<RakeFileTransform>().Any().ShouldBeTrue();

            var contents = readFile("rakefile");

            contents.ShouldContain("# the solution is MySolution at src/MySolution.sln");
        }

        [Test]
        public void read_gems_from_solution()
        {
            executePlan(x => x.AddTemplate("Simple"));
            thePlan.Steps.OfType<GemReference>().Any().ShouldBeTrue();

            var contents = readFile("Gemfile");

            contents.ShouldContain("gem \"rake\", \"~>10.0\"");
            contents.ShouldContain("gem \"fuburake\", \"~>0.5\"");
        }

        [Test]
        public void gitignore_directive_in_solution()
        {
            executePlan(x => x.AddTemplate("Simple"));

            readFile(".gitignore").ShouldContain("bin");
            readFile(".gitignore").ShouldContain("obj");
        }

        [Test]
        public void create_a_default_project()
        {
            executePlan(x => {
                x.AddTemplate("Simple");
                x.AddProjectRequest(new ProjectRequest { Name = "MyProject", Template = "Simple" });
            });

            assertFileExists("src", "MyProject", "MyProject.csproj");
        }

        [Test]
        public void create_a_project_from_a_csproj_file_template()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");
                x.AddProjectRequest(new ProjectRequest { Name = "MyProject", Template="CustomProjFile"  });
            });

            assertFileExists("src", "MyProject", "MyProject.csproj");

            var csProjFile = CsProjFile.LoadFrom("integrated".AppendPath("src", "MyProject", "MyProject.csproj"));
            csProjFile.Find<AssemblyReference>("System.Data").ShouldNotBeNull(); // this reference is NOT in the default 
        }

        [Test]
        public void copy_project_folder()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");
                x.AddProjectRequest(new ProjectRequest { Name = "MyProject", Template = "Simple" });
            });

            assertDirectoryExists("src", "MyProject", "Random");
        }

        [Test]
        public void copy_project_file()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");
                x.AddProjectRequest(new ProjectRequest { Name = "MyProject", Template = "Simple" });
            });

            assertFileExists("src","MyProject","random.txt");
        }

        [Test]
        public void copy_project_folders_in_alteration()
        {
            executePlan(x => {
                x.AddTemplate("Simple");

                var projectRequest = new ProjectRequest {Name = "MyProject", Template = "Simple"};
                projectRequest.AddAlteration("Assets");

                x.AddProjectRequest(projectRequest);
            });

            assertDirectoryExists("src", "MyProject", "content");
            assertDirectoryExists("src", "MyProject", "content", "scripts");
            assertDirectoryExists("src", "MyProject", "content", "styles");
            assertDirectoryExists("src", "MyProject", "content", "images");
        }

        [Test]
        public void copy_file_in_project_alteration()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");

                var projectRequest = new ProjectRequest { Name = "MyProject", Template = "Simple" };
                projectRequest.AddAlteration("Assets");

                x.AddProjectRequest(projectRequest);
            });

            assertFileExists("src", "MyProject", "content", "images", "fake.bmp");
        }

        [Test]
        public void rake_transform_from_within_project()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");
                x.AddProjectRequest(new ProjectRequest { Name = "MyProject", Template = "Simple" });
            });

            var contents = readFile("rakefile");
            contents.ShouldContain("# the project is MyProject at src/MyProject/MyProject.csproj");
        }

        [Test]
        public void pick_up_AssemblyInfo_transform_from_project_alteration()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");

                var projectRequest = new ProjectRequest { Name = "MyProject", Template = "Simple" };
                projectRequest.AddAlteration("FubuBottle");

                x.AddProjectRequest(projectRequest);
            });

            var contents = readFile("src", "MyProject", "Properties", "AssemblyInfo.cs");
            contents.ShouldContain("using FubuMVC.Core;");
            contents.ShouldContain("[assembly: FubuModule]");
        }

        [Test]
        public void gitignore_directive_in_project()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");

                var projectRequest = new ProjectRequest { Name = "MyProject", Template = "Simple" };
                projectRequest.AddAlteration("FubuBottle");

                x.AddProjectRequest(projectRequest);
            });

            readFile(".gitignore").ShouldContain("Gemfile.lock");
        }

        [Test]
        public void assembly_reference_in_project()
        {
            executePlan(x =>
            {
                x.AddTemplate("Simple");

                var projectRequest = new ProjectRequest { Name = "MyProject", Template = "Simple" };

                x.AddProjectRequest(projectRequest);
            });

            assertFileExists("src", "MyProject", "MyProject.csproj");

            var csProjFile = CsProjFile.LoadFrom("integrated".AppendPath("src", "MyProject", "MyProject.csproj"));
            csProjFile.Find<AssemblyReference>("System.Configuration").ShouldNotBeNull(); // this reference is NOT in the default 
            csProjFile.Find<AssemblyReference>("System.Security").ShouldNotBeNull(); // this reference is NOT in the default 
        }
    }
}