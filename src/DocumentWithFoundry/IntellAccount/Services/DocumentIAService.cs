using Azure.AI.DocumentIntelligence;
using Azure;
using System.Text.Json;
using System.Text;

namespace IntellAccount.Services;
public class DocumentIAService(string key, string endpoint)
{
    public async Task Recognize(string filePath)
    {
        AzureKeyCredential credential = new AzureKeyCredential(key);
        DocumentIntelligenceClient client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

        Uri uriSource = new Uri(filePath);
        var content = new AnalyzeDocumentContent()
        {
            UrlSource = uriSource
        };

        Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", content);
        AnalyzeResult result = operation.Value;

        foreach (DocumentPage page in result.Pages)
        {
            Console.WriteLine($"Document Page {page.PageNumber} has {page.Lines.Count} line(s), {page.Words.Count} word(s)," +
                $" and {page.SelectionMarks.Count} selection mark(s).");

            for (int i = 0; i < page.Lines.Count; i++)
            {
                DocumentLine line = page.Lines[i];

                Console.WriteLine($"  Line {i}:");
                Console.WriteLine($"    Content: '{line.Content}'");

                Console.Write("    Bounding polygon, with points ordered clockwise:");
                for (int j = 0; j < line.Polygon.Count; j += 2)
                {
                    Console.Write($" ({line.Polygon[j]}, {line.Polygon[j + 1]})");
                }

                Console.WriteLine();
            }

            for (int i = 0; i < page.SelectionMarks.Count; i++)
            {
                DocumentSelectionMark selectionMark = page.SelectionMarks[i];

                Console.WriteLine($"  Selection Mark {i} is {selectionMark.State}.");
                Console.WriteLine($"    State: {selectionMark.State}");

                Console.Write("    Bounding polygon, with points ordered clockwise:");
                for (int j = 0; j < selectionMark.Polygon.Count; j++)
                {
                    Console.Write($" ({selectionMark.Polygon[j]}, {selectionMark.Polygon[j + 1]})");
                }

                Console.WriteLine();
            }
        }

        for (int i = 0; i < result.Paragraphs.Count; i++)
        {
            DocumentParagraph paragraph = result.Paragraphs[i];

            Console.WriteLine($"Paragraph {i}:");
            Console.WriteLine($"  Content: {paragraph.Content}");

            if (paragraph.Role != null)
            {
                Console.WriteLine($"  Role: {paragraph.Role}");
            }
        }

        foreach (DocumentStyle style in result.Styles)
        {
            // Check the style and style confidence to see if text is handwritten.
            // Note that value '0.8' is used as an example.

            bool isHandwritten = style.IsHandwritten.HasValue && style.IsHandwritten == true;

            if (isHandwritten && style.Confidence > 0.8)
            {
                Console.WriteLine($"Handwritten content found:");

                foreach (DocumentSpan span in style.Spans)
                {
                    var handwrittenContent = result.Content.Substring(span.Offset, span.Length);
                    Console.WriteLine($"  {handwrittenContent}");
                }
            }
        }

        for (int i = 0; i < result.Tables.Count; i++)
        {
            DocumentTable table = result.Tables[i];

            Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");

            foreach (DocumentTableCell cell in table.Cells)
            {
                Console.WriteLine($"  Cell ({cell.RowIndex}, {cell.ColumnIndex}) is a '{cell.Kind}' with content: {cell.Content}");
            }
        }
    }

    public async Task<string> RecognizeAsJson(string filePath)
    {
        AzureKeyCredential credential = new AzureKeyCredential(key);
        DocumentIntelligenceClient client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

        Uri uriSource = new Uri(filePath);
        var content = new AnalyzeDocumentContent()
        {
            UrlSource = uriSource
        };

        Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", content);
        AnalyzeResult result = operation.Value;

        return JsonSerializer.Serialize(result.Paragraphs);
    }

    public async Task<string> RecognizeAsString(string filePath, string documentType = "prebuilt-layout")
    {
        AzureKeyCredential credential = new AzureKeyCredential(key);
        DocumentIntelligenceClient client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

        Uri uriSource = new Uri(filePath);
        var content = new AnalyzeDocumentContent()
        {
            UrlSource = uriSource
        };

        Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, documentType, content);
        AnalyzeResult result = operation.Value;

        return ProcessDocument(result);
    }


    public string ProcessDocument(AnalyzeResult result)
    {
        var output = new StringBuilder();

        foreach (DocumentPage page in result.Pages)
        {
            output.AppendLine($"Document Page {page.PageNumber} has {page.Lines.Count} line(s), {page.Words.Count} word(s)," +
                              $" and {page.SelectionMarks.Count} selection mark(s).");

            for (int i = 0; i < page.Lines.Count; i++)
            {
                DocumentLine line = page.Lines[i];
                output.AppendLine($"  Line {i}:");
                output.AppendLine($"    Content: '{line.Content}'");

                output.Append("    Bounding polygon, with points ordered clockwise:");
                for (int j = 0; j < line.Polygon.Count; j += 2)
                {
                    output.Append($" ({line.Polygon[j]}, {line.Polygon[j + 1]})");
                }

                output.AppendLine();
            }

            for (int i = 0; i < page.SelectionMarks.Count; i++)
            {
                DocumentSelectionMark selectionMark = page.SelectionMarks[i];
                output.AppendLine($"  Selection Mark {i} is {selectionMark.State}.");
                output.AppendLine($"    State: {selectionMark.State}");

                output.Append("    Bounding polygon, with points ordered clockwise:");
                for (int j = 0; j < selectionMark.Polygon.Count; j++)
                {
                    output.Append($" ({selectionMark.Polygon[j]}, {selectionMark.Polygon[j + 1]})");
                }

                output.AppendLine();
            }
        }

        for (int i = 0; i < result.Paragraphs.Count; i++)
        {
            DocumentParagraph paragraph = result.Paragraphs[i];
            output.AppendLine($"Paragraph {i}:");
            output.AppendLine($"  Content: {paragraph.Content}");

            if (paragraph.Role != null)
            {
                output.AppendLine($"  Role: {paragraph.Role}");
            }
        }

        foreach (DocumentStyle style in result.Styles)
        {
            bool isHandwritten = style.IsHandwritten.HasValue && style.IsHandwritten == true;

            if (isHandwritten && style.Confidence > 0.8)
            {
                output.AppendLine("Handwritten content found:");

                foreach (DocumentSpan span in style.Spans)
                {
                    var handwrittenContent = result.Content.Substring(span.Offset, span.Length);
                    output.AppendLine($"  {handwrittenContent}");
                }
            }
        }

        for (int i = 0; i < result.Tables.Count; i++)
        {
            DocumentTable table = result.Tables[i];
            output.AppendLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");

            foreach (DocumentTableCell cell in table.Cells)
            {
                output.AppendLine($"  Cell ({cell.RowIndex}, {cell.ColumnIndex}) is a '{cell.Kind}' with content: {cell.Content}");
            }
        }

        return output.ToString();
    }
}
