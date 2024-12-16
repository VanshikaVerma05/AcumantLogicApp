



//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
//using CsvHelper;
//using System.Globalization;
//using System.IO;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Xml;
//using System.Linq;

//public class BlobTriggeredFunction
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;

//    public BlobTriggeredFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
//    {
//        _logger = loggerFactory.CreateLogger<BlobTriggeredFunction>();
//        _configuration = configuration;
//    }

//    [Function("BlobTriggerFunction")]
//    public async Task RunAsync([BlobTrigger("csvfiles-01/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, string name)
//    {
//        _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {blobStream.Length} Bytes");

//        // Read the CSV file into a list of records
//        var records = new List<Dictionary<string, string>>();
//        using (var reader = new StreamReader(blobStream))
//        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
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

//        // Load the mapping configuration from the JSON file
//        var mappingConfig = await LoadMappingConfigAsync();

//        // Create XML based on the CSV data and mapping configuration
//        var xmlDoc = CreateXmlFromCsv(records, mappingConfig);

//        // Output the resulting XML (for logging or other purposes)
//        string xmlContent = xmlDoc.OuterXml;
//        _logger.LogInformation($"Transformed XML:\n{xmlContent}");
//    }

//    private async Task<Dictionary<string, List<Mapping>>> LoadMappingConfigAsync()
//    {
//        var mappingFilePath = _configuration["MappingFilePath"]; // Ensure this is correctly set in your settings

//        try
//        {
//            using (var stream = new FileStream(mappingFilePath, FileMode.Open))
//            using (var reader = new StreamReader(stream))
//            {
//                var json = await reader.ReadToEndAsync();

//                // Deserialize JSON to MappingFile object
//                var mappings = JsonConvert.DeserializeObject<MappingFile>(json);

//                // Group mappings by TermOfPayment, ensuring multiple mappings can exist for the same TermOfPayment
//                var groupedMappings = mappings?.Mappings
//                    .GroupBy(m => m.TermOfPayment)
//                    .ToDictionary(g => g.Key, g => g.ToList());

//                return groupedMappings ?? new Dictionary<string, List<Mapping>>();
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error loading mapping config: {ex.Message}");
//            throw;
//        }
//    }

//    private XmlDocument CreateXmlFromCsv(List<Dictionary<string, string>> records, Dictionary<string, List<Mapping>> mappingConfig)
//    {
//        var xmlDoc = new XmlDocument();
//        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
//        xmlDoc.AppendChild(xmlDeclaration);

//        XmlElement rootElement = xmlDoc.CreateElement("Payments");
//        xmlDoc.AppendChild(rootElement);

//        foreach (var record in records)
//        {
//            XmlElement paymentElement = xmlDoc.CreateElement("Payment");
//            rootElement.AppendChild(paymentElement);

//            string dataValue = record.ContainsKey("Data_value") ? record["Data_value"] : string.Empty;
//            string period = record.ContainsKey("Period") ? record["Period"] : string.Empty;
//            string subject = record.ContainsKey("Subject") ? record["Subject"] : string.Empty;
//            string termOfPayment = record.ContainsKey("Term Of Payment") ? record["Term Of Payment"] : string.Empty;
//            string methodOfPayment = record.ContainsKey("Method Of Payment") ? record["Method Of Payment"] : string.Empty;
//            string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

//            _logger.LogInformation($"Extracted Data: DataValue={dataValue}, Period={period}, Subject={subject}, TermOfPayment={termOfPayment}, MethodOfPayment={methodOfPayment}");

//            // Apply transformation only if status is "F" and we have matching mappings for both fields
//            string transformedTermOfPayment = termOfPayment;
//            string transformedMethodOfPayment = methodOfPayment;

//            if (status == "F")
//            {
//                transformedTermOfPayment = TransformField("TermOfPayment", termOfPayment, methodOfPayment, mappingConfig);
//                transformedMethodOfPayment = TransformField("MethodOfPayment", methodOfPayment, termOfPayment, mappingConfig);
//            }

//            // Add elements to XML
//            AddXmlElement(xmlDoc, paymentElement, "Status", status);
//            AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", transformedTermOfPayment);
//            AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", transformedMethodOfPayment);
//            AddXmlElement(xmlDoc, paymentElement, "MappedTerm", transformedTermOfPayment); // Assuming it's same as TermOfPayment for now
//            AddXmlElement(xmlDoc, paymentElement, "MappedMethod", transformedMethodOfPayment); // Assuming it's same as MethodOfPayment for now
//        }

//        return xmlDoc;
//    }

//    private string TransformField(string fieldName, string fieldValue, string otherFieldValue, Dictionary<string, List<Mapping>> mappingConfig)
//    {
//        // Loop through the mapping configuration to find the matching mapping
//        foreach (var mappingEntry in mappingConfig)
//        {
//            // Look for a matching TermOfPayment and MethodOfPayment pair
//            var matchingMapping = mappingEntry.Value.FirstOrDefault(m =>
//                (fieldName == "TermOfPayment" && m.TermOfPayment == fieldValue && m.MethodOfPayment == otherFieldValue) ||
//                (fieldName == "MethodOfPayment" && m.MethodOfPayment == fieldValue && m.TermOfPayment == otherFieldValue)
//            );

//            // If a matching mapping is found
//            if (matchingMapping != null)
//            {
//                // Return the corresponding mapped term or method
//                if (fieldName == "TermOfPayment")
//                    return matchingMapping.MappedTerm;
//                if (fieldName == "MethodOfPayment")
//                    return matchingMapping.MappedMethod;
//            }
//        }

//        // If no mapping is found, return the original field value
//        return fieldValue;
//    }

//    private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string value)
//    {
//        XmlElement element = xmlDoc.CreateElement(elementName);
//        element.InnerText = value;
//        parentElement.AppendChild(element);
//    }
//}

//// Classes to represent the mapping configuration

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


//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
//using CsvHelper;
//using System.Globalization;
//using System.IO;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Xml;
//using System.Linq;

//public class BlobTriggeredFunction
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;

//    public BlobTriggeredFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
//    {
//        _logger = loggerFactory.CreateLogger<BlobTriggeredFunction>();
//        _configuration = configuration;
//    }

//    [Function("BlobTriggerFunction")]
//    public async Task RunAsync([BlobTrigger("csvfiles-01/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, string name)
//    {
//        _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {blobStream.Length} Bytes");

//        // Read the CSV file into a list of records
//        var records = new List<Dictionary<string, string>>();
//        using (var reader = new StreamReader(blobStream))
//        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
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

//        // Load the mapping configuration from the JSON file
//        var mappingConfig = await LoadMappingConfigAsync();

//        // Create XML based on the CSV data and mapping configuration
//        var xmlDoc = CreateXmlFromCsv(records, mappingConfig);

//        // Output the resulting XML (for logging or other purposes)
//        string xmlContent = xmlDoc.OuterXml;
//        _logger.LogInformation($"Transformed XML:\n{xmlContent}");
//    }

//    private async Task<Dictionary<string, List<Mapping>>> LoadMappingConfigAsync()
//    {
//        var mappingFilePath = _configuration["MappingFilePath"]; // Ensure this is correctly set in your settings

//        try
//        {
//            using (var stream = new FileStream(mappingFilePath, FileMode.Open))
//            using (var reader = new StreamReader(stream))
//            {
//                var json = await reader.ReadToEndAsync();

//                // Deserialize JSON to MappingFile object
//                var mappings = JsonConvert.DeserializeObject<MappingFile>(json);

//                // Group mappings by TermOfPayment, ensuring multiple mappings can exist for the same TermOfPayment
//                var groupedMappings = mappings?.Mappings
//                    .GroupBy(m => m.TermOfPayment)
//                    .ToDictionary(g => g.Key, g => g.ToList());

//                return groupedMappings ?? new Dictionary<string, List<Mapping>>();
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error loading mapping config: {ex.Message}");
//            throw;
//        }
//    }

//    private XmlDocument CreateXmlFromCsv(List<Dictionary<string, string>> records, Dictionary<string, List<Mapping>> mappingConfig)
//    {
//        var xmlDoc = new XmlDocument();
//        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
//        xmlDoc.AppendChild(xmlDeclaration);

//        XmlElement rootElement = xmlDoc.CreateElement("Payments");
//        xmlDoc.AppendChild(rootElement);

//        foreach (var record in records)
//        {
//            XmlElement paymentElement = xmlDoc.CreateElement("Payment");
//            rootElement.AppendChild(paymentElement);

//            string dataValue = record.ContainsKey("Data_value") ? record["Data_value"] : string.Empty;
//            string period = record.ContainsKey("Period") ? record["Period"] : string.Empty;
//            string subject = record.ContainsKey("Subject") ? record["Subject"] : string.Empty;
//            string termOfPayment = record.ContainsKey("Term Of Payment") ? record["Term Of Payment"] : string.Empty;
//            string methodOfPayment = record.ContainsKey("Method Of Payment") ? record["Method Of Payment"] : string.Empty;
//            string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

//            _logger.LogInformation($"Extracted Data: DataValue={dataValue}, Period={period}, Subject={subject}, TermOfPayment={termOfPayment}, MethodOfPayment={methodOfPayment}");

//            // Apply transformation only if status is "F" and we have matching mappings for both fields
//            string transformedTermOfPayment = termOfPayment;
//            string transformedMethodOfPayment = methodOfPayment;

//            if (status == "F")
//            {
//                transformedTermOfPayment = TransformField("TermOfPayment", termOfPayment, methodOfPayment, mappingConfig);
//                transformedMethodOfPayment = TransformField("MethodOfPayment", methodOfPayment, termOfPayment, mappingConfig);
//            }

//            // Add elements to XML
//            AddXmlElement(xmlDoc, paymentElement, "Status", status);
//            AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", transformedTermOfPayment);
//            AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", transformedMethodOfPayment);
//            AddXmlElement(xmlDoc, paymentElement, "MappedTerm", transformedTermOfPayment); // Assuming it's same as TermOfPayment for now
//            AddXmlElement(xmlDoc, paymentElement, "MappedMethod", transformedMethodOfPayment); // Assuming it's same as MethodOfPayment for now
//        }

//        return xmlDoc;
//    }

//    private string TransformField(string fieldName, string fieldValue, string otherFieldValue, Dictionary<string, List<Mapping>> mappingConfig)
//    {
//        // Loop through the mapping configuration to find the matching mapping
//        foreach (var mappingEntry in mappingConfig)
//        {
//            // Look for a matching TermOfPayment and MethodOfPayment pair
//            var matchingMapping = mappingEntry.Value.FirstOrDefault(m =>
//                (fieldName == "TermOfPayment" && m.TermOfPayment == fieldValue && m.MethodOfPayment == otherFieldValue) ||
//                (fieldName == "MethodOfPayment" && m.MethodOfPayment == fieldValue && m.TermOfPayment == otherFieldValue)
//            );

//            // If a matching mapping is found
//            if (matchingMapping != null)
//            {
//                // Return the corresponding mapped term or method
//                if (fieldName == "TermOfPayment")
//                    return matchingMapping.MappedTerm;
//                if (fieldName == "MethodOfPayment")
//                    return matchingMapping.MappedMethod;
//            }
//        }

//        // If no mapping is found, return the original field value
//        return fieldValue;
//    }

//    private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string value)
//    {
//        XmlElement element = xmlDoc.CreateElement(elementName);
//        element.InnerText = value;
//        parentElement.AppendChild(element);
//    }
//}

//// Classes to represent the mapping configuration
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


//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
//using System.IO;
//using System.Collections.Generic;
//using System.Xml;
//using System.Linq;
//using System.Threading.Tasks;

//public class BlobTriggeredFunction
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;

//    public BlobTriggeredFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
//    {
//        _logger = loggerFactory.CreateLogger<BlobTriggeredFunction>();
//        _configuration = configuration;
//    }

//    [Function("BlobTriggerFunction")]
//    public async Task RunAsync([BlobTrigger("csvfiles-01/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, string name)
//    {
//        _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {blobStream.Length} Bytes");

//        // Read the CSV file into a list of records
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

//        // Load the mapping configuration from the JSON file
//        var mappingConfig = await LoadMappingConfigAsync();

//        // Create XML based on the CSV data and mapping configuration
//        var xmlDoc = CreateXmlFromCsv(records, mappingConfig);

//        // Output the resulting XML (for logging or other purposes)
//        string xmlContent = xmlDoc.OuterXml;
//        _logger.LogInformation($"Transformed XML:\n{xmlContent}");
//    }

//    private async Task<List<Mapping>> LoadMappingConfigAsync()
//    {
//        var mappingFilePath = _configuration["MappingFilePath"]; // Ensure this is correctly set in your settings

//        try
//        {
//            using (var stream = new FileStream(mappingFilePath, FileMode.Open))
//            using (var reader = new StreamReader(stream))
//            {
//                var json = await reader.ReadToEndAsync();

//                // Deserialize JSON to MappingFile object
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
//            XmlElement paymentElement = xmlDoc.CreateElement("Payment");
//            rootElement.AppendChild(paymentElement);

//            string dataValue = record.ContainsKey("Data_value") ? record["Data_value"] : string.Empty;
//            string period = record.ContainsKey("Period") ? record["Period"] : string.Empty;
//            string subject = record.ContainsKey("Subject") ? record["Subject"] : string.Empty;
//            string termOfPayment = record.ContainsKey("Term Of Payment") ? record["Term Of Payment"] : string.Empty;
//            string methodOfPayment = record.ContainsKey("Method Of Payment") ? record["Method Of Payment"] : string.Empty;
//            string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

//            _logger.LogInformation($"Extracted Data: DataValue={dataValue}, Period={period}, Subject={subject}, TermOfPayment={termOfPayment}, MethodOfPayment={methodOfPayment}");

//            // Directly apply transformation based on the mapping file
//            var transformed = ApplyMapping(termOfPayment, methodOfPayment, mappingConfig);

//            // Add elements to XML
//            AddXmlElement(xmlDoc, paymentElement, "Status", status);
//            AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", transformed.MappedTerm);
//            AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", transformed.MappedMethod);
//            AddXmlElement(xmlDoc, paymentElement, "MappedTerm", transformed.MappedTerm);  // Using transformed TermOfPayment
//            AddXmlElement(xmlDoc, paymentElement, "MappedMethod", transformed.MappedMethod); // Using transformed MethodOfPayment
//        }

//        return xmlDoc;
//    }

//    private (string MappedTerm, string MappedMethod) ApplyMapping(string termOfPayment, string methodOfPayment, List<Mapping> mappingConfig)
//    {
//        // Match the mapping where both TermOfPayment and MethodOfPayment are either equal to the CSV values or empty
//        var matchingMapping = mappingConfig.FirstOrDefault(m =>
//            (string.IsNullOrEmpty(m.TermOfPayment) || m.TermOfPayment == termOfPayment) &&
//            (string.IsNullOrEmpty(m.MethodOfPayment) || m.MethodOfPayment == methodOfPayment)
//        );

//        // If a matching mapping is found, return the corresponding MappedTerm and MappedMethod
//        if (matchingMapping != null)
//        {
//            return (matchingMapping.MappedTerm, matchingMapping.MappedMethod);
//        }

//        // If no matching mapping is found, return the original term and method (no transformation)
//        return (termOfPayment, methodOfPayment);
//    }

//    //private (string MappedTerm, string MappedMethod) ApplyMapping(string termOfPayment, string methodOfPayment, List<Mapping> mappingConfig)
//    //{
//    //    // Directly lookup and apply transformation based on TermOfPayment and MethodOfPayment
//    //    var matchingMapping = mappingConfig.FirstOrDefault(m =>
//    //        m.TermOfPayment == termOfPayment && m.MethodOfPayment == methodOfPayment
//    //    );

//    //    // If a matching mapping is found, return the corresponding MappedTerm and MappedMethod
//    //    if (matchingMapping != null)
//    //    {
//    //        return (matchingMapping.MappedTerm, matchingMapping.MappedMethod);
//    //    }

//    //    // If no matching mapping is found, return the original term and method (no transformation)
//    //    return (termOfPayment, methodOfPayment);
//    //}


//    private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string value)
//    {
//        XmlElement element = xmlDoc.CreateElement(elementName);
//        element.InnerText = value;
//        parentElement.AppendChild(element);
//    }
//}

//// Classes to represent the mapping configuration

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

