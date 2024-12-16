
//using OfficeOpenXml;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;
//using System.Xml;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using System.Net.Http;
//using System.Text;

//public class BlobTriggeredFunction
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;
//    private readonly HttpClient _httpClient;
//    public BlobTriggeredFunction(ILoggerFactory loggerFactory, IConfiguration configuration, HttpClient httpClient)
//    {
//        _logger = loggerFactory.CreateLogger<BlobTriggeredFunction>();
//        _configuration = configuration;
//        _httpClient = httpClient;
//    }

//    [Function("BlobTriggerFunction")]
//    public async Task RunAsync([BlobTrigger("csvfiles-01/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, string name)
//    {
//        _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {blobStream.Length} Bytes");


//        var records = new List<Dictionary<string, string>>();
//        using (var reader = new StreamReader(blobStream))
//        using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
//        {
//            var csvRecords = csv.GetRecords<dynamic>();
//            foreach (var record in csvRecords)
//            {
//                var recordDict = new Dictionary<string, string>();
//                foreach (var property in record)
//                {
//                    recordDict.Add(property.Key, property.Value?.ToString() ?? string.Empty);
//                }
//                records.Add(recordDict);
//            }
//        }
//        var mappingConfig = await LoadMappingConfigAsync();


//        var xmlDoc = CreateXmlFromCsv(records, mappingConfig);


//        string xmlContent = xmlDoc.OuterXml;
//        _logger.LogInformation($"Transformed XML:\n{xmlContent}");
//    }

//    private async Task<List<Mapping>> LoadMappingConfigAsync()
//    {
//        var mappingFilePath = _configuration["MappingFilePath"];

//        try
//        {
//            using (var stream = new FileStream(mappingFilePath, FileMode.Open))
//            using (var reader = new StreamReader(stream))
//            {
//                var json = await reader.ReadToEndAsync();


//                var mappings = JsonConvert.DeserializeObject<MappingFile>(json);

//                return mappings?.Mappings ?? new List<Mapping>();
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error loading mapping config: {ex.Message}");
//            throw;
//        }
//    }

//    private XmlDocument CreateXmlFromCsv(List<Dictionary<string, string>> records, List<Mapping> mappingConfig)
//    {
//        var xmlDoc = new XmlDocument();
//        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
//        xmlDoc.AppendChild(xmlDeclaration);

//        XmlElement rootElement = xmlDoc.CreateElement("Payments");
//        xmlDoc.AppendChild(rootElement);

//        foreach (var record in records)
//        {
//            string dataValue = record.ContainsKey("Data_value") ? record["Data_value"] : string.Empty;
//            string period = record.ContainsKey("Period") ? record["Period"] : string.Empty;
//            string subject = record.ContainsKey("Subject") ? record["Subject"] : string.Empty;
//            string termOfPayment = record.ContainsKey("Term Of Payment") ? record["Term Of Payment"] : string.Empty;
//            string methodOfPayment = record.ContainsKey("Method Of Payment") ? record["Method Of Payment"] : string.Empty;
//            string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

//            _logger.LogInformation($"Extracted Data: DataValue={dataValue}, Period={period}, Subject={subject}, TermOfPayment={termOfPayment}, MethodOfPayment={methodOfPayment}");
//            foreach (var mapping in mappingConfig)
//            {
//                XmlElement paymentElement = xmlDoc.CreateElement("Payment");
//                rootElement.AppendChild(paymentElement);

//                AddXmlElement(xmlDoc, paymentElement, "Status", string.Empty);
//                AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", mapping.TermOfPayment);
//                AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", mapping.MethodOfPayment);
//                AddXmlElement(xmlDoc, paymentElement, "MappedTerm", mapping.MappedTerm);
//                AddXmlElement(xmlDoc, paymentElement, "MappedMethod", mapping.MappedMethod);
//            }
//        }
//        return xmlDoc;
//    }

//    private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string value)
//    {
//        XmlElement element = xmlDoc.CreateElement(elementName);
//        element.InnerText = value;
//        parentElement.AppendChild(element);
//    }
//    private async Task SendXmlToLogicAppAsync(string xmlContent)
//    {
//        var logicAppEndpoint = _configuration["LogicAppEndpoint"];

//        try
//        {
//            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

//            var response = await _httpClient.PostAsync(logicAppEndpoint, content);

//            if (response.IsSuccessStatusCode)
//            {
//                _logger.LogInformation("Successfully sent XML to Logic App.");
//            }
//            else
//            {
//                _logger.LogError($"Failed to send XML to Logic App. Status code: {response.StatusCode}");
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error sending XML to Logic App: {ex.Message}");
//            throw;
//        }
//    }
//}
//public class MappingFile
//{
//    public List<Mapping> Mappings { get; set; }
//}

//public class Mapping
//{
//    public string TermOfPayment { get; set; }
//    public string MethodOfPayment { get; set; }
//    public string MappedTerm { get; set; }
//    public string MappedMethod { get; set; }
//}


//==================================================>
using OfficeOpenXml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
public class BlobTriggeredFunction
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public BlobTriggeredFunction(ILoggerFactory loggerFactory, IConfiguration configuration, HttpClient httpClient)
    {
        _logger = loggerFactory.CreateLogger<BlobTriggeredFunction>();
        _configuration = configuration;
        _httpClient = httpClient;
    }

    [Function("BlobTriggerFunction")]
    public async Task RunAsync([BlobTrigger("csvfiles-01/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, string name)
    {
        _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {blobStream.Length} Bytes");

        var records = new List<Dictionary<string, string>>();
        using (var reader = new StreamReader(blobStream))
        using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
        {
            var csvRecords = csv.GetRecords<dynamic>();
            foreach (var record in csvRecords)
            {
                var recordDict = new Dictionary<string, string>();
                foreach (var property in record)
                {
                    recordDict.Add(property.Key, property.Value?.ToString() ?? string.Empty);
                }
                records.Add(recordDict);
            }
        }


        var mappingConfig = await LoadMappingConfigAsync();

        var xmlDoc = CreateXmlFromCsv(records, mappingConfig);


        string jsonContent = ConvertXmlToJson(xmlDoc.OuterXml);
        _logger.LogInformation($"Transformed JSON:\n{jsonContent}");


        await SendJsonToLogicAppAsync(jsonContent);
    }

    private async Task<List<Mapping>> LoadMappingConfigAsync()
    {
        var mappingFilePath = _configuration["MappingFilePath"];

        try
        {
            using (var stream = new FileStream(mappingFilePath, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();

                var mappings = JsonConvert.DeserializeObject<MappingFile>(json);

                return mappings?.Mappings ?? new List<Mapping>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading mapping config: {ex.Message}");
            throw;
        }
    }

    //private XmlDocument CreateXmlFromCsv(List<Dictionary<string, string>> records, List<Mapping> mappingConfig)
    //{
    //    var xmlDoc = new XmlDocument();
    //    XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
    //    xmlDoc.AppendChild(xmlDeclaration);

    //    XmlElement rootElement = xmlDoc.CreateElement("Payments");
    //    xmlDoc.AppendChild(rootElement);


        //    foreach (var record in records)
        //    {
        //        string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

        //        _logger.LogInformation($"Processing record with Status: {status}");


        //        foreach (var mapping in mappingConfig)
        //        {
        //            XmlElement paymentElement = xmlDoc.CreateElement("Payment");
        //            rootElement.AppendChild(paymentElement);


        //            AddXmlElement(xmlDoc, paymentElement, "Status", status);
        //            AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", mapping.TermOfPayment);
        //            AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", mapping.MethodOfPayment);
        //            AddXmlElement(xmlDoc, paymentElement, "MappedTerm", mapping.MappedTerm);
        //            AddXmlElement(xmlDoc, paymentElement, "MappedMethod", mapping.MappedMethod);
        //        }
        //    }
        //    return xmlDoc;
        //}

        private XmlDocument CreateXmlFromCsv(List<Dictionary<string, string>> records, List<Mapping> mappingConfig)
        {
            var xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            XmlElement rootElement = xmlDoc.CreateElement("Payments");
            xmlDoc.AppendChild(rootElement);

            foreach (var record in records)
            {
                string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

                _logger.LogInformation($"Processing record with Status: {status}");

                foreach (var mapping in mappingConfig)
                {
                   
                    XmlElement paymentElement = xmlDoc.CreateElement("Payment");
                    rootElement.AppendChild(paymentElement);

                    AddXmlElement(xmlDoc, paymentElement, "Status", status);
                    AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", mapping.TermOfPayment);
                    AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", mapping.MethodOfPayment);
                    AddXmlElement(xmlDoc, paymentElement, "MappedTerm", mapping.MappedTerm);
                    AddXmlElement(xmlDoc, paymentElement, "MappedMethod", mapping.MappedMethod);
                }
            }

            return xmlDoc;
        }


        private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string value)
    {
        XmlElement element = xmlDoc.CreateElement(elementName);
        element.InnerText = value;
        parentElement.AppendChild(element);
    }


    private string ConvertXmlToJson(string xmlContent)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        string json = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented);
        return json;
    }
    private async Task SendJsonToLogicAppAsync(string jsonContent)
    {
        var logicAppEndpoint = _configuration["LogicAppEndpoint"];

        try
        {
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(logicAppEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent JSON to Logic App.");
            }
            else
            {
                _logger.LogError($"Failed to send JSON to Logic App. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending JSON to Logic App: {ex.Message}");
            throw;
        }
    }
}
public class MappingFile
{
    public List<Mapping> Mappings { get; set; }
}

public class Mapping
{
    public string TermOfPayment { get; set; }
    public string MethodOfPayment { get; set; }
    public string MappedTerm { get; set; }
    public string MappedMethod { get; set; }
}
