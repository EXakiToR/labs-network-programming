using System;


class Program
{
    static int Main(string[] args)
    {
        // accept only the number of threads. 
        // file is loceted in "./numbers.txt", yes, hardcoded intensionally

        if (args.Length != 1 || !int.TryParse(args[0], out int numberOfThreads))
        {
            Console.Error.WriteLine("Error: Expected exactly one integer argument.");
            return 1;
        }

        // for now it is like this. Will change lately to normal. Rihgt now it is placeholder.
        Console.WriteLine($"Number: {numberOfThreads}");
        return 0;
    }
}