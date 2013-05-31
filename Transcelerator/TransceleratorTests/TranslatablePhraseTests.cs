// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
#endregion
// 
// File: TranslatablePhraseTests.cs
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AddInSideViews;
using NUnit.Framework;
using Rhino.Mocks;

namespace SIL.Transcelerator
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
    /// Tests the TranslatablePhrase implementation
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
    public class TranslatablePhraseTests : PhraseTranslationTestBase
	{
	    [SetUp]
	    public override void Setup()
	    {
            base.Setup();
	        DummyKeyTermRenderingInfo.s_ktRenderings = m_dummyKtRenderings;
	        TranslatablePhrase.s_helper = MockRepository.GenerateMock<IPhraseTranslationHelper>();
	    }

	    #region PartPatternMatches tests
        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests the PartPatternMatches method with phrases that consist of a single part.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void PartPatternMatches_SinglePart()
        {
            var cat = m_sections.Items[0].Categories[0];

            AddTestQuestion(cat, "Wuzzee?", "wuzzee");
            AddTestQuestion(cat, "Wuzzee!", "wuzzee");
            AddTestQuestion(cat, "As a man thinks in his heart, how is he?", "as a man thinks in his heart how is he");

            var phrases = (new QuestionProvider(GetParsedQuestions())).ToList();

            Assert.IsTrue(phrases[0].PartPatternMatches(phrases[1]));
            Assert.IsTrue(phrases[1].PartPatternMatches(phrases[0]));
            Assert.IsTrue(phrases[1].PartPatternMatches(phrases[1]));
            Assert.IsFalse(phrases[2].PartPatternMatches(phrases[1]));
            Assert.IsFalse(phrases[2].PartPatternMatches(phrases[0]));
            Assert.IsFalse(phrases[0].PartPatternMatches(phrases[2]));
            Assert.IsFalse(phrases[1].PartPatternMatches(phrases[2]));
            Assert.IsTrue(phrases[2].PartPatternMatches(phrases[2]));
        }

        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests the PartPatternMatches method with phrases that consist of multiple parts.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void PartPatternMatches_OneTranslatablePartOneKeyTerm()
        {
            AddMockedKeyTerm("wunkers");
            AddMockedKeyTerm("punkers");

            var cat = m_sections.Items[0].Categories[0];

            AddTestQuestion(cat, "Wuzzee wunkers?", "wuzzee", "kt:wunkers");
            AddTestQuestion(cat, "Wuzzee punkers.", "wuzzee", "kt:punkers");
            AddTestQuestion(cat, "Wuzzee wunkers!", "wuzzee", "kt:wunkers");
            AddTestQuestion(cat, "Wunkers wuzzee!", "kt:wunkers", "wuzzee");
            AddTestQuestion(cat, "A dude named punkers?", "a dude named", "kt:punkers");

            var phrases = (new QuestionProvider(GetParsedQuestions())).ToList();

            Assert.IsTrue(phrases[0].PartPatternMatches(phrases[1]));
            Assert.IsTrue(phrases[0].PartPatternMatches(phrases[2]));
            Assert.IsFalse(phrases[0].PartPatternMatches(phrases[3]));
            Assert.IsFalse(phrases[0].PartPatternMatches(phrases[4]));

            Assert.IsTrue(phrases[1].PartPatternMatches(phrases[2]));
            Assert.IsFalse(phrases[1].PartPatternMatches(phrases[3]));
            Assert.IsFalse(phrases[1].PartPatternMatches(phrases[4]));

            Assert.IsFalse(phrases[2].PartPatternMatches(phrases[3]));
            Assert.IsFalse(phrases[2].PartPatternMatches(phrases[4]));

            Assert.IsFalse(phrases[3].PartPatternMatches(phrases[4]));
        }
        #endregion
        
        #region TranslatablePhrase.FindTermRenderingInUse tests
        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void TestFindTermRenderingInUse_Present()
        {
            IKeyTerm ktGod = AddMockedKeyTerm("God", "Dios");
            IKeyTerm ktPaul = AddMockedKeyTerm("Paul", "paulo", "Pablo", "luaP");
            IKeyTerm ktHave = AddMockedKeyTerm("have", "tenemos");
            IKeyTerm ktSay = AddMockedKeyTerm("say", "dice");

            var cat = m_sections.Items[0].Categories[0];

            AddTestQuestion(cat, "What did God tell Paul?",
                "what did", "kt:god", "tell", "kt:paul");
            AddTestQuestion(cat, "What does Paul say we have to give to God?",
                "what does", "kt:paul", "kt:say", "we", "kt:have", "to give to", "kt:god");

            var phrases = (new QuestionProvider(GetParsedQuestions())).ToList();

            TranslatablePhrase phrase1 = phrases[0];
            TranslatablePhrase phrase2 = phrases[1];

            phrase1.Translation = "\u00BFQue\u0301 le dijo Dios a Pablo?";
            phrase2.Translation = "\u00BFQue\u0301 dice luaP que tenemos que dar a Dios?";

            SubstringDescriptor sd;

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
            Assert.AreEqual(13, sd.Offset);
            Assert.AreEqual(4, sd.Length);

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
            Assert.AreEqual(20, sd.Offset);
            Assert.AreEqual(5, sd.Length);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
            Assert.AreEqual(10, sd.Offset);
            Assert.AreEqual(4, sd.Length);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktSay, 0));
            Assert.AreEqual(5, sd.Offset);
            Assert.AreEqual(4, sd.Length);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktHave, 0));
            Assert.AreEqual(19, sd.Offset);
            Assert.AreEqual(7, sd.Length);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
            Assert.AreEqual(37, sd.Offset);
            Assert.AreEqual(4, sd.Length);
        }

        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void TestFindTermRenderingInUse_SomeMissing()
        {
            IKeyTerm ktGod = AddMockedKeyTerm("God", "Dios");
            IKeyTerm ktPaul = AddMockedKeyTerm("Paul", "Pablo");
            IKeyTerm ktHave = AddMockedKeyTerm("have", "tenemos");
            IKeyTerm ktSay = AddMockedKeyTerm("say", "dice");

            var cat = m_sections.Items[0].Categories[0];

            AddTestQuestion(cat, "What did God tell Paul?",
                "what did", "kt:god", "tell", "kt:paul");
            AddTestQuestion(cat, "What does Paul say we have to give to God?",
                "what does", "kt:paul", "kt:say", "we", "kt:have", "to give to", "kt:god");

            var phrases = (new QuestionProvider(GetParsedQuestions())).ToList();

            TranslatablePhrase phrase1 = phrases[0];
            TranslatablePhrase phrase2 = phrases[1];

            phrase1.Translation = "\u00BFQue\u0301 le dijo Jehovah a Pablo?";
            phrase2.Translation = "Pi\u0301dale ayuda a Bill.";

            SubstringDescriptor sd;

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
            Assert.IsNull(sd);

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
            Assert.AreEqual(23, sd.Offset);
            Assert.AreEqual(5, sd.Length);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
            Assert.IsNull(sd);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktSay, 0));
            Assert.IsNull(sd);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktHave, 0));
            Assert.IsNull(sd);

            sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
            Assert.IsNull(sd);
        }

        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void TestFindTermRenderingInUse_RepeatedTerms()
        {
            IKeyTerm ktGod = AddMockedKeyTerm("God", "Dios");
            IKeyTerm ktPaul = AddMockedKeyTerm("Paul", "Pablo");

            var cat = m_sections.Items[0].Categories[0];

            AddTestQuestion(cat, "What did God tell Paul?/What was Paul told by God?",
                "what did", "kt:god", "tell", "kt:paul", "what was", "kt:paul", "told by", "kt:god");

            var phrases = (new QuestionProvider(GetParsedQuestions())).ToList();

            TranslatablePhrase phrase1 = phrases[0];

            phrase1.Translation = "\u00BFQue\u0301 le dijo Dios a Pablo?/\u00BFQue\u0301 le fue dicho a Pablo por Dios?";

            SubstringDescriptor sd;

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
            Assert.AreEqual(13, sd.Offset);
            Assert.AreEqual(4, sd.Length);
            int endOfLastOccurrenceOfGod = sd.EndOffset;

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
            Assert.AreEqual(20, sd.Offset);
            Assert.AreEqual(5, sd.Length);
            int endOfLastOccurrenceOfPaul = sd.EndOffset;

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, endOfLastOccurrenceOfGod));
            Assert.AreEqual(57, sd.Offset);
            Assert.AreEqual(4, sd.Length);

            sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, endOfLastOccurrenceOfPaul));
            Assert.AreEqual(47, sd.Offset);
            Assert.AreEqual(5, sd.Length);
        }
        #endregion

        #region UserTransSansOuterPunctuation tests
        [Test]
        public void UserTransSansOuterPunctuation_LeadingPunctuation_GetsRemoved()
        {
            TranslatablePhrase p = new TranslatablePhrase("That is so cool!");
            p.Translation = "!Cool is so that";
            Assert.AreEqual("Cool is so that", p.UserTransSansOuterPunctuation);
        }

        [Test]
        public void UserTransSansOuterPunctuation_TrailingPunctuation_GetsRemoved()
        {
            TranslatablePhrase p = new TranslatablePhrase("That is so cool!");
            p.Translation = "Cool is so that!";
            Assert.AreEqual("Cool is so that", p.UserTransSansOuterPunctuation);
        }

        [Test]
        public void UserTransSansOuterPunctuation_MultiplePunctuationCharacters_GetsRemoved()
        {
            TranslatablePhrase p = new TranslatablePhrase("That is so cool!");
            p.Translation = "\".Cool is so that!?";
            Assert.AreEqual("Cool is so that", p.UserTransSansOuterPunctuation);
        }
        #endregion

        #region Translation tests
        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void SetTranslation_UserTranslation_IsComposed()
        {
            var cat = m_sections.Items[0].Categories[0];

            AddTestQuestion(cat, "What did God tell Paul?", "what did god tell paul");

            var phrases = (new QuestionProvider(GetParsedQuestions())).ToList();

            TranslatablePhrase phrase1 = phrases[0];

            phrase1.Translation = "\u00BFQue\u0301 le dijo Dios a Pablo?";

            Assert.AreEqual("\u00BFQue\u0301 le dijo Dios a Pablo?".Normalize(NormalizationForm.FormC),
                phrase1.Translation);
            Assert.IsTrue(phrase1.HasUserTranslation);
        }

        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void GetTranslation_QuestionWithNoUserTranslation_GetsStringWithInitialAndFinalPunctuation()
        {
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.Question)).Return(";");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.Question)).Return("?");
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.StatementOrImperative)).Return("");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.StatementOrImperative)).Return(".");

            TranslatablePhrase phrase1 = new TranslatablePhrase("Why don't I have a translation?");

            Assert.AreEqual(";?", phrase1.Translation);
            Assert.IsFalse(phrase1.HasUserTranslation);
        }

        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void GetTranslation_StatementWithNoUserTranslation_GetsStringWithInitialAndFinalPunctuation()
        {
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.Question)).Return(";");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.Question)).Return("?");
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.StatementOrImperative)).Return("");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.StatementOrImperative)).Return(".");

            TranslatablePhrase phrase1 = new TranslatablePhrase("I don't have a translation.");

            Assert.AreEqual(".", phrase1.Translation);
            Assert.IsFalse(phrase1.HasUserTranslation);
        }

        /// ------------------------------------------------------------------------------------
        /// <summary>
        /// Tests finding renderings of key terms when there is exactly one occurrence of each
        /// term in a particular phrase and one of the renderings is present in the translation.
        /// </summary>
        /// ------------------------------------------------------------------------------------
        [Test]
        public void GetTranslation_UnknownWithNoUserTranslation_GetsStringWithInitialAndFinalPunctuation()
        {
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.Question)).Return(";");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.Question)).Return("?");
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.StatementOrImperative)).Return("");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.StatementOrImperative)).Return(".");
            TranslatablePhrase.s_helper.Stub(h => h.InitialPunctuationForType(TypeOfPhrase.Unknown)).Return("-");
            TranslatablePhrase.s_helper.Stub(h => h.FinalPunctuationForType(TypeOfPhrase.Unknown)).Return("-");

            TranslatablePhrase phrase1 = new TranslatablePhrase("-OR-");

            Assert.AreEqual("--", phrase1.Translation);
            Assert.IsFalse(phrase1.HasUserTranslation);
            
            TranslatablePhrase phrase2 = new TranslatablePhrase("Oops, I forgot the puctuation");

            Assert.AreEqual("--", phrase2.Translation);
            Assert.IsFalse(phrase1.HasUserTranslation);
        }
        #endregion

        #region Private helper methods
        /// ------------------------------------------------------------------------------------
	    /// <summary>
	    /// Adds a test question to the given category and adds info about key terms and parts
	    /// to dictionaries used by GetParsedQuestions. Note that items in the parts array will
	    /// be treated as translatable parts unless prefixed with "kt:", in which case they
	    /// will be treated as key terms (corresponding key terms must be added by calling
	    /// AddMockedKeyTerm.
	    /// </summary>
	    /// ------------------------------------------------------------------------------------
	    private void AddTestQuestion(Category cat, string text, params string[] parts)
	    {
	        var q = new TestQ(text, "A", 1, 1, GetParsedParts(parts));
	        cat.Questions.Add(q);
	    }
	    #endregion
	}

    /// ----------------------------------------------------------------------------------------
    /// <summary>
    /// Dummy class so we don't have to use a real list box
    /// </summary>
    /// ----------------------------------------------------------------------------------------
    internal class DummyKeyTermRenderingInfo : ITermRenderingInfo
    {
        internal static Dictionary<string, List<string>> s_ktRenderings;

        #region Implementation of ITermRenderingInfo
        public IEnumerable<string> Renderings { get; private set; }

        public int EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm { get; set; }

        public DummyKeyTermRenderingInfo(IKeyTerm kt, int endOffsetOfPrev)
        {
            Renderings = s_ktRenderings[kt.Id];
            EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm = endOffsetOfPrev;
        }
        #endregion

    }
}
