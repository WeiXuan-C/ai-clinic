using System;
using System.Collections.Generic;

namespace DoFactory.GangOfFour.Strategy.NETOptimized
{
    /// <summary>
    /// MainApp startup class for .NET optimized 
    /// Strategy Design Pattern.
    /// </summary>
    class MainApp
    {
        /// <summary>
        /// Entry point into console application.
        /// </summary>
        static void Main()
        {
            // Two contexts following different strategies
            SortedList studentRecords = new SortedList();

            studentRecords.Add(new Student("Samual","154-33-2009"));
            studentRecords.Add(new Student("Jimmy", "487-43-1665"));
            studentRecords.Add(new Student("Sandra","655-00-2944"));
            studentRecords.Add(new Student("Vivek", "133-98-8399"));
            studentRecords.Add(new Student("Anna",  "760-94-9844"));

            studentRecords.SortStrategy = new QuickSort();
            studentRecords.Sort();

            studentRecords.SortStrategy = new ShellSort();
            studentRecords.Sort();

            studentRecords.SortStrategy = new MergeSort();
            studentRecords.Sort();

            // Wait for user
            Console.Read();
        }
    }

    // "Strategy"

    interface ISortStrategy
    {
        void Sort(List<Student> list);
    }

    // "ConcreteStrategy" 

    class QuickSort : ISortStrategy
    {
        public void Sort(List<Student> list)
        {
            // Call overloaded Sort
            Sort(list, 0, list.Count - 1);
            Console.WriteLine("QuickSorted list ");
        }

        // Recursively sort
        private void Sort(List<Student> list, int left, int right)
        {
            int lhold = left;
            int rhold = right;
            
            // Use a random pivot
            Random random = new Random();
            int pivot = random.Next(left, right);
            Swap(list, pivot, left);
            pivot = left;
            left++;
            while (right >= left)
            {
                int compareleft = list[left].Name.CompareTo(list[pivot].Name);
                int compareright = list[right].Name.CompareTo(list[pivot].Name);

                if ((compareleft >= 0) && (compareright < 0))
                {
                    Swap(list, left, right);
                }
                else
                {
                    if (compareleft >= 0)
                    {
                        right--;
                    }
                    else
                    {
                        if (compareright < 0)
                        {
                            left++;
                        }
                        else
                        {
                            right--;
                            left++;
                        }
                    }
                }
            }
            Swap(list, pivot, right);
            pivot = right;

            if (pivot > lhold) Sort(list, lhold, pivot);
            if (rhold > pivot + 1) Sort(list, pivot + 1, rhold);
        }

        // Swap helper function
        private void Swap(List<Student> list, int left, int right)
        {
            Student temp = list[right];
            list[right] = list[left];
            list[left] = temp;
        }
    }

    // "ConcreteStrategy" 

    class ShellSort : ISortStrategy
    {
        public void Sort(List<Student> list)
        { 
            // ShellSort();  not-implemented
            Console.WriteLine("ShellSorted list ");
        }
    }

    // "ConcreteStrategy" 

    class MergeSort : ISortStrategy
    {
        public void Sort(List<Student> list)
        {
            // MergeSort(); not-implemented
            Console.WriteLine("MergeSorted list ");
        }
    }

    // "Context" 

    class SortedList
    {
        private List<Student> list = new List<Student>();
        private ISortStrategy sortstrategy;

        // Property
        public ISortStrategy SortStrategy
        {
            set
            {
                this.sortstrategy = value;
            }
        }

        public void Add(Student student)
        {
            list.Add(student);
        }

        public void Sort()
        {
            sortstrategy.Sort(list);

            // Display results
            foreach (Student student in list)
            {
                Console.WriteLine(" " + student.Name);
            }
            Console.WriteLine();
        }
    }

    class Student
    {
        private string name;
        private string Ssn;

        public Student(string name, string Ssn)
        {
            this.name = name;
            this.Ssn = Ssn;
        }
        public string Name
        {
            get { return name; }
        }
        public string SSN
        {
            get { return Ssn; }
        }
    }
}
