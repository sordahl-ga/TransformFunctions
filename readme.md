# TransformFunctions

Health Data Platform Supporting Transformation Azure Functions

+ api/HL7toJSON -- Converts HL7 Message contained in posted body to a JSON Object for easy access and readability

+ api/NLPExtractEntitiesHttp -- Runs the NLP Pipeline on any Binary or Text Body posted to function.  The NLP Pipeline will perform OCR on binary files and extract text, process text through the CTAKES NLP Engine for term identification and concept coding. Optionally with request parameter updatesearch=true update the configured Azure Search index with the content and term/concept coding facets.

+ api/TransformHL7SaveToBlob -- Converts HL7 Message contained in posted body to a JSON Object and saves it to the configured blob store.

+ api/TransformHL7SaveToDB -- Converts HL7 Message contained in posted body to a JSON Object and saves it to the configured CosmosDB collection.

+ api/TransformCCDSaveToDB -- Concerts CCDA Documents contained in posted body to JSON Object and saves it to the configured CosmosDB collection.

+ UpdateSearchIndexDiagReport -- Change Feed listener to run NLP processor on HL7 Messages, extract reports and perform the NLP Pipeline. The NLP Pipeline will perform OCR on process text through the CTAKES NLP Engine for term identification and concept coding and update the configured Azure Search index with the content and term/concept coding facets in the report.

+ UpdateFhirServer -- Change feed listener to convert received HL7 messages to FHIR and Update the configured FHIR Server

+ NLPExtractEntitiesFile -- Runs the NLP Pipeline on any file dropped in the configured storage account container.  The NLP Pipeline will perform OCR on binary files and extract text, process text through the CTAKES NLP Engine for term identification and concept coding and update the configured Azure Search index with the content and term/concept coding facets.

+ HL7BatchIngest -- Extracts single or bulk hl7 messages contained in single files or compressed tar balls, converts each message to a JSON Object and saves it to the configured CosmosDB Collection

##Required Application Settings

The application settings that need to be configured for the transform functions:
```
	"StorageAccount": "<Storage Account Connection String>",
    "StorageAccountBlob": "<Storage account hl7 blob container>",
    "CosmosDBConnection": "<CosmosDB Connection String>",
	"CosmosDBNAME": "<CosmosDB Transform Database Name>",
    "CosmosCCDCollection": "<The name of the cosmosdb collection to hold transformed CCD Documents>",
    "CosmosHL7Collection": "<The name of the cosmosdb collection to hold transformed hl7 messages">,
    "TIKAServerurl": "<The URL of TIKA Server for OCR>",
    "CTAKESServerURL": "<The URL of CTAKES Server for NLP Entity Extraction>",
    "CTAKESFormat": "XML",
    "CTAKESUMLSUser": "<The NIH UMLS User Name for Ontology Mappings>",
    "CTAKESUMLSPassword": "<The NIH UMLS User Password for Ontology Mappings>",
    "SearchServiceName": "<The name of the search service>",
    "SearchServiceKey": "<The search service access key>",
    "SearchServiceIndexName": "<The name of the search index to contain documents>",
    "UseMetaDataFieldNames": "no",
```

## Authors

* **Steven Ordahl** - Microsoft HLS Apps and Infrastructure Cloud Architect
