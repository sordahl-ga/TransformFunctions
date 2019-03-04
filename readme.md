# TransformFunctions

Health Data Platform Supporting Transformation Azure Functions

+ api/HL7toJSON -- Converts HL7 Message contained in posted body to a JSON Object for easy access and readability

+ api/NLPExtractEntitiesHttp -- Runs the NLP Pipeline on any Binary or Text Body posted to function.  The NLP Pipeline will perform OCR on binary files and extract text, process text through the CTAKES NLP Engine for term identification and concept coding. Optionally with request parameter updatesearch=true update the configured Azure Search index with the content and term/concept coding facets.

+ api/TransformHL7SaveToBlob -- Converts HL7 Message contained in posted body to a JSON Object and saves it to the configured blob store.

+ api/TransformHL7SaveToDB -- Converts HL7 Message contained in posted body to a JSON Object and saves it to the configured CosmosDB collection.

+ UpdateSearchIndexDiagReport -- Change Feed listener to run NLP processor on HL7 Messages, extract reports and perform the NLP Pipeline. The NLP Pipeline will perform OCR on process text through the CTAKES NLP Engine for term identification and concept coding and update the configured Azure Search index with the content and term/concept coding facets in the report.

+ UpdateFhirServer -- Change feed listener to convert received HL7 messages to FHIR and Update the configured FHIR Server

+ NLPExtractEntitiesFile -- Runs the NLP Pipeline on any file dropped in the configured storage account container.  The NLP Pipeline will perform OCR on binary files and extract text, process text through the CTAKES NLP Engine for term identification and concept coding and update the configured Azure Search index with the content and term/concept coding facets.


## Authors

* **Steven Ordahl** - Microsoft HLS Apps and Infrastructure Cloud Architect
