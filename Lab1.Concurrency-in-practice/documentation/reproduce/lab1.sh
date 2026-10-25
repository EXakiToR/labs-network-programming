#!/usr/bin/env bash
set -euo pipefail
lab_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$lab_root/code"
command_name="${1:-help}"
shift || true
case "$command_name" in
  build)
    dotnet build TaskOneA.cs --output artifacts/TaskOneA --nologo
    dotnet build TaskOneC.cs --output artifacts/TaskOneC --nologo
    dotnet build TaskTwo.cs --output artifacts/TaskTwo --nologo
    dotnet build "$lab_root/documentation/reproduce/TaskOneBBenchmark.cs" --output artifacts/ReportTaskOneB --nologo
    ;;
  task1a|task1c)
    [[ $# -eq 1 ]] || { echo 'Supply the number of threads/workers.' >&2; exit 1; }
    program=TaskOneA
    [[ "$command_name" != task1c ]] || program=TaskOneC
    dotnet "artifacts/$program/$program.dll" "$1"
    ;;
  task1b)
    [[ $# -ge 2 && $# -le 3 ]] || { echo 'Usage: lab1.sh task1b WORKERS VERSION [RUN]' >&2; exit 1; }
    echo "Version=$2 Workers=$1 Run=${3:-1}"
    dotnet artifacts/ReportTaskOneB/TaskOneBBenchmark.dll "$2" "$1" "$lab_root/code/numbers.txt"
    ;;
  task2)
    [[ $# -ge 1 && $# -le 2 ]] || { echo 'Usage: lab1.sh task2 WORKERS [USB_DIRECTORY]' >&2; exit 1; }
    dotnet artifacts/TaskTwo/TaskTwo.dll "$1" "${2:-/Volumes/ESD-USB}"
    ;;
  menu)
    dotnet run --file Program.cs
    ;;
  *)
    echo 'Usage: bash documentation/reproduce/lab1.sh build|menu|task1a|task1b|task1c|task2 [arguments]'
    [[ "$command_name" == help ]] || exit 1
    ;;
esac
