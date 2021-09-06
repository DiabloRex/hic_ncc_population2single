# hic_ncc_population2single
Covert populational hic ncc format matrix to single cell matrix

## Usage: 
hic_ncc_population2single [1.Input file] [2.output file] [3.threshold] [4.method]

method: 0 -> PointMax, 1 -> EdgeMax, 2 -> TotalRandom

## Example:
hic_ncc_population2single O48.ncc O48.single.point.ncc 2 0

## Excutable:
/bin/hic_ncc_population2single_linux -> for linux machine

/bin/hic_ncc_population2single_win64 -> for x64 windows machine

/bin/hic_ncc_population2single_win86 -> for x86 windows machine

## Require
.net core 3.1: https://dotnet.microsoft.com/download/dotnet/3.1

## Help:

if you have any question, please contact me at: yanmeng(a)sibs.ac.cn. replace (a) with @.
