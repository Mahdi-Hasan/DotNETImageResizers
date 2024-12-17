using DotNETImageResizer;


string inputFolderPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Inputs"));
string outputReportPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Report"));
string outputFilesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\CompressedImages\"));

var results = new ImageCompressor().RunBenchmarksAsync(inputFolderPath, outputFilesPath);

new ReportGenerator().GenerateReport(results, outputReportPath);
