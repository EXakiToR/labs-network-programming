# Lab1 Concurrency in Practice

This project implements the laboratory tasks described in "Concurrency in Practice".

Build:

  dotnet build

Run examples:

  dotnet run -- generate data 1000000
  dotnet run -- split data
  dotnet run -- task1a data\numbers.txt Results/task1a.csv
  dotnet run -- task1b data\numbers.txt Results/task1b.csv
  dotnet run -- task1c data\numbers.txt Results/task1c.csv

Plot results (requires Python with pandas and matplotlib):

  python plot_results.py Results/task1a.csv Results/task1a

Notes:
- Generating 50,000,000 integers requires disk space and time; the generator defaults to 1,000,000 for quick tests.
- For Task 2, copy the 8 part*.txt files to a USB drive and run the task2 command pointing at the USB folder.
