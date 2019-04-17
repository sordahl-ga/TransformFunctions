/* 
* 2018 Microsoft Corp
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS”
* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
* THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
* ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
* FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
* HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
* OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformFunctions
{
    
    [SerializePropertyNamesAsCamelCase]
    public partial class IndexSchema
    {
        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.StandardLucene)]
        public string content { get; set; }

        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string SearchId { get; set; }
        [IsFilterable]
        public string StorageId { get; set; }
        [IsFilterable]
        public string StorageLocation { get; set; }
        [IsSearchable, IsFilterable, IsFacetable]
        public string DocumentType { get; set; }
        [IsSearchable, IsFilterable, IsFacetable]
        public string[] medical_mentions { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] medical_mention_concepts { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] medical_mention_codes { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] disease_disorders { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] disease_disorder_concepts { get; set; }
        [IsSearchable, IsFilterable, IsFacetable]
        public string[] disease_disorder_codes { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] sign_symptoms { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] sign_symptom_concepts { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] sign_symptom_codes { get; set; }


        [IsSearchable, IsFilterable, IsFacetable]
        public string[] anatomical_sites { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] anatomical_site_concepts { get; set; }
        [IsSearchable, IsFilterable, IsFacetable]
        public string[] anatomical_site_codes { get; set; }

    }
    public class CTakesRequest
    {
        public string Content { get; set; }
        public string Format { get; set; }

        public string UMLSUser { get; set; }

        public string UMLSPassword { get; set; }

        public string CTAKESUrl { get; set; }

    }
    public class MedicalEntities
    {
        public string Id { get; set; }
        public string Location { get; set; }
        public string DocumentType { get; set; }
        public string ParsedText { get; set; }
        public List<Term> DiseaseDisorderList { get; set; }
        public List<Term> MedicationMentionList { get; set; }
        public List<Term> SignSymptomMentionList { get; set; }
        public List<Term> AnatomicalSiteMentionList { get; set; }

        public List<OntologyConcept> DiseaseDisorderConceptList { get; set; }
        public List<OntologyConcept> MedicationMentionConceptList { get; set; }
        public List<OntologyConcept> SignSymptomMentionConceptList { get; set; }
        public List<OntologyConcept> AnatomicalSiteMentionConceptList { get; set; }

        public List<Concept> ConceptNameDictionary { get; set; }
    }

    public class Concept
    {
        public int ConceptId { get; set; }
        public string ConceptName { get; set; }
        public string CUI { get; set; }
        public string Code { get; set; }
        public string CodingSchema {get;set;}
    }

    public class Term
    {
        public Guid termId { get; set; }
        public string term { get; set; }
      
    }
    public class OntologyConcept
    {
        public Guid termId { get; set; }
        public Guid conceptId { get; set; }
        public string ontologyConcept { get; set; }
    }

}
