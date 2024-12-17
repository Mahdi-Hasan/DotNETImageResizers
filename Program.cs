using DotNETImageResizer;


string inputFolderPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Inputs"));
string outputReportPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Report"));

var results = new ImageCompressor().RunBenchmarksAsync(inputFolderPath);

new ReportGenerator().GenerateReport(results, outputReportPath);
