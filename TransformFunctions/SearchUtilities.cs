using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Logging;
namespace TransformFunctions
{
    public class SearchUtilities
    {
        private string SearchServiceName = Utilities.GetEnvironmentVariable("SearchServiceName");
        private string SearchServiceKey = Utilities.GetEnvironmentVariable("SearchServiceKey");
        private string SearchServiceIndexName = Utilities.GetEnvironmentVariable("SearchServiceIndexName");
        private ILogger log;
        private ISearchServiceClient searchClient;
        private ISearchIndexClient indexClient;
        public SearchUtilities(ILogger log)
        {
            this.log = log;
            searchClient = new SearchServiceClient(SearchServiceName, new SearchCredentials(SearchServiceKey));
            indexClient = searchClient.Indexes.GetClient(SearchServiceIndexName);
            CreateorUpdateIndexSchema();
        }
        public void ProcessConceptList(List<string> namelist, List<string> codelist, List<OntologyConcept> conceptList, List<Concept> conceptDictionary)
        {
            foreach (var concept in conceptList.Select(x => x.ontologyConcept).Distinct())
            {
                var entry = conceptDictionary.Where(x => x.ConceptId == Convert.ToInt32(concept)).First();
                namelist.Add(entry.ConceptName);
                codelist.Add(entry.CodingSchema + ":" + entry.Code);
            }
        }
        public void UploadMedicalEntities(MedicalEntities[] medicalEntities)
        {

            // Upload the specified entity to Azure Search
            // Note it is much more efficient to upload content in batches
            var uploadBatch = new List<IndexSchema>();
            foreach (MedicalEntities medicaEntities in medicalEntities) { 
                // Convert Concept ID's to actual Concept names
                var MedicationMentionNames = new List<string>();
                var AnatomicalSiteNames = new List<string>();
                var DiseaseDisorderNames = new List<string>();
                var SignSymptomNames = new List<string>();
                var MedicationMentionCodes = new List<string>();
                var AnatomicalSiteCodes = new List<string>();
                var DiseaseDisorderCodes = new List<string>();
                var SignSymptomCodes = new List<string>();
                ProcessConceptList(MedicationMentionNames, MedicationMentionCodes, medicaEntities.MedicationMentionConceptList,medicaEntities.ConceptNameDictionary);
                ProcessConceptList(AnatomicalSiteNames, AnatomicalSiteCodes, medicaEntities.AnatomicalSiteMentionConceptList, medicaEntities.ConceptNameDictionary);
                ProcessConceptList(DiseaseDisorderNames, DiseaseDisorderCodes, medicaEntities.DiseaseDisorderConceptList, medicaEntities.ConceptNameDictionary);
                ProcessConceptList(SignSymptomNames, SignSymptomCodes, medicaEntities.SignSymptomMentionConceptList, medicaEntities.ConceptNameDictionary);

                var indexDoc = new IndexSchema();
                indexDoc.content = medicaEntities.ParsedText;
                indexDoc.SearchId = Guid.NewGuid().ToString();
                indexDoc.StorageId = medicaEntities.Id;
                indexDoc.StorageLocation = medicaEntities.Location;
                indexDoc.DocumentType = medicaEntities.DocumentType;
                indexDoc.medical_mentions = medicaEntities.MedicationMentionList.Select(x => x.term).Distinct().ToArray();
                indexDoc.medical_mention_concepts = MedicationMentionNames.Distinct().ToArray();
                indexDoc.medical_mention_codes = MedicationMentionCodes.Distinct().ToArray();
                indexDoc.sign_symptoms = medicaEntities.SignSymptomMentionList.Select(x => x.term).Distinct().ToArray();
                indexDoc.sign_symptom_concepts = SignSymptomNames.Distinct().ToArray();
                indexDoc.sign_symptom_codes = SignSymptomCodes.Distinct().ToArray();
                indexDoc.anatomical_sites = medicaEntities.AnatomicalSiteMentionList.Select(x => x.term).Distinct().ToArray();
                indexDoc.anatomical_site_concepts = AnatomicalSiteNames.Distinct().ToArray();
                indexDoc.anatomical_site_codes = AnatomicalSiteCodes.Distinct().ToArray();
                indexDoc.disease_disorders = medicaEntities.DiseaseDisorderList.Select(x => x.term).Distinct().ToArray();
                indexDoc.disease_disorder_concepts = DiseaseDisorderNames.Distinct().ToArray();
                indexDoc.disease_disorder_codes = DiseaseDisorderCodes.Distinct().ToArray();
                uploadBatch.Add(indexDoc);

            }
            try
            {

                var batch = IndexBatch.MergeOrUpload(uploadBatch);
                indexClient.Documents.Index(batch);
                uploadBatch.Clear();
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);

            }

        }
        private void CreateorUpdateIndexSchema()
        {
            try
            {
                var definition = new Index()
                {
                    Name = SearchServiceIndexName,
                    Fields = FieldBuilder.BuildForType<IndexSchema>()
                };
                searchClient.Indexes.CreateOrUpdate(definition);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return;
            }

        }
    }
}
