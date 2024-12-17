using DotNETImageResizer;


const string inputFolderPath = @"D:\PIHR\Demo\DotNETImageResizers";
const string outputReportPath = @"D:\PIHR\Demo\Output";

var compressor = new ImageCompressor();
var results =compressor.RunBenchmarksAsync(inputFolderPath);
compressor.GenerateReport(results, outputReportPath);