using System;
using System.Collections.Generic;

namespace DoFactory.GangOfFour.Factory.NETOptimized
{
    /// <summary>
    /// MainApp startup class for .NET optimized 
    /// Factory Method Design Pattern.
    /// </summary>
    class MainApp
    {
        /// <summary>
        /// Entry point into console application.
        /// </summary>
        static void Main()
        {
            // Note: document constructors call Factory Method
            List<Document> documents = new List<Document>();
            documents.Add(new Resume());
            documents.Add(new Report());

            // Display document pages
            foreach (Document document in documents)
            {
                Console.WriteLine( document + "--" );
                foreach (Page page in document.Pages)
                {
                    Console.WriteLine(" " + page);
                }
                Console.WriteLine();
            }

            // Wait for user
            Console.Read();        
        }
    }

    // "Product"

    abstract class Page
    {
        // Override. Display class name
        public override string ToString()
        {
            return this.GetType().Name;
        }
    }

    // "ConcreteProduct"

    class SkillsPage : Page
    {
    }

    // "ConcreteProduct"

    class EducationPage : Page
    {
    }

    // "ConcreteProduct"

    class ExperiencePage : Page
    {
    }

    // "ConcreteProduct"

    class IntroductionPage : Page
    {
    }

    // "ConcreteProduct"

    class ResultsPage : Page
    {
    }

    // "ConcreteProduct"

    class ConclusionPage : Page
    {
    }

    // "ConcreteProduct"

    class SummaryPage : Page
    {
    }

    // "ConcreteProduct"

    class BibliographyPage : Page
    {
    }

    // "Creator"

    abstract class Document
    {
        private List<Page> pages = new List<Page>();

        // Constructor invokes Factory Method
        public Document()
        {
            this.CreatePages();
        }

        // Property
        public List<Page> Pages
        {
            get{ return pages; }
        }

        // Factory Method
        public abstract void CreatePages();

        // Override
        public override string ToString()
        {
            return this.GetType().Name;
        }
    }

    // "ConcreteCreator"

    class Resume : Document
    {
        // Factory Method implementation
        public override void CreatePages()
        {
            Pages.Add(new SkillsPage());
            Pages.Add(new EducationPage());
            Pages.Add(new ExperiencePage());
        }
    }

    // "ConcreteCreator"

    class Report : Document
    {
        // Factory Method implementation
        public override void CreatePages()
        {
            Pages.Add(new IntroductionPage());
            Pages.Add(new ResultsPage());
            Pages.Add(new ConclusionPage());
            Pages.Add(new SummaryPage());
            Pages.Add(new BibliographyPage());
        }
    }
}
