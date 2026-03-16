#r "packages/iText7.8.0.2/lib/net8.0/iText.Kernel.dll"
using System;
using iText.Kernel.Pdf;

string filePath = @"C:\Users\admin\Desktop\合并文档 1.pdf";
using var reader = new PdfReader(filePath);
using var doc = new PdfDocument(reader);
var page = doc.GetPage(1);
Console.WriteLine($"MediaBox: {page.GetMediaBox().GetWidth()} x {page.GetMediaBox().GetHeight()}");
Console.WriteLine($"CropBox: {page.GetCropBox().GetWidth()} x {page.GetCropBox().GetHeight()}");
Console.WriteLine($"TrimBox: {page.GetTrimBox().GetWidth()} x {page.GetTrimBox().GetHeight()}");
Console.WriteLine($"BleedBox: {page.GetBleedBox().GetWidth()} x {page.GetBleedBox().GetHeight()}");
Console.WriteLine($"Rotation: {page.GetRotation()}");
