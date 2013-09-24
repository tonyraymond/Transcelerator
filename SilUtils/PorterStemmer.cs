// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International.
// <copyright from='2011' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved (derivitive work only).
//
//		Distributable under the terms of the MIT License (http://sil.mit-license.org/)
//		This file incorporates work covered by the following permission notice:
//		The software is completely free for any purpose, unless notes at the head of
//		the program text indicates otherwise (which is rare). In any case, the notes
//		about licensing are never more restrictive than the BSD License.
// </copyright>
#endregion
//
// File: PorterStemmer.cs
//
//Porter stemmer in CSharp, based on the Java port. The original paper is in
//
//    Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
//    no. 3, pp 130-137,
//
//See also http://www.tartarus.org/~martin/PorterStemmer
//

//
//History:
//
//Release 1
//
//Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
//The words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
//is then out outside the bounds of b.
//
//Release 2
//
//Similarly,
//
//Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
//'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
//b[j] is then outside the bounds of b.
//
//Release 3
//
//Considerably revised 4/9/00 in the light of many helpful suggestions
//from Brian Goetz of Quiotix Corporation (brian@quiotix.com).
//
//Release 4
// Made more suitable for use in .Net
//
//Cheers Leif
//
// Release 5
// Added ability to strip off possessives & prevent removal of final "ee" (TomB)
// ---------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stemmer, implementing the Porter Stemming Algorithm
	/// The Stemmer class transforms a word into its root form.  The input word can be provided
	/// a character at time (by calling add()), or at once by calling one of the various
	/// stem(something) methods.
	/// </summary>
	/// ----------------------------------------------------------------------------------------  
	public interface StemmerInterface 
	{
		string stemTerm( string s );
	}

	[ClassInterface( ClassInterfaceType.None )]
	public class PorterStemmer : StemmerInterface
	{
		private char[] b;
		private int i,     /* offset into b */
			i_end, /* offset to end of stemmed word */
			j, k;
		private bool changed;
		private static int INC = 200;

		/* unit of size whereby b is increased */
		
		public PorterStemmer() 
		{
			b = new char[INC];
			i = 0;
			i_end = 0;
			changed = false;
		}

		public bool StagedStemming { get; set; }

		/* Implementation of the .NET interface - added as part of realease 4 (Leif) */
		public string stemTerm( string s )
		{
			setTerm( s );
			stem();
			return getTerm();
		}

		/*
			SetTerm and GetTerm have been simply added to ease the 
			interface with other lanaguages. They replace the add functions 
			and toString function. This was done because the original functions stored
			all stemmed words (and each time a new word was added, the buffer would be
			re-copied each time, making it quite slow). Now, The class interface 
			that is provided simply accepts a term and returns its stem, 
			instead of storing all stemmed words.
			(Leif)
		*/
		void setTerm( string s)
		{
			i = s.Length;
			char[] new_b = new char[i];
			for (int c = 0; c < i; c++)
			new_b[c] = s[c];

			b = new_b;
			changed = false;
		}

		public string getTerm()
		{
			return new String(b, 0, i_end);
		}

		/* Old interface to the class - left for posterity. However, it is not
		 * used when accessing the class via .NET (Leif)*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a character to the word being stemmed.  When you are finished
		/// adding characters, you can call stem(void) to stem the word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void add(char ch) 
		{
			if (i == b.Length) 
			{
				char[] new_b = new char[i+INC];
				for (int c = 0; c < i; c++)
					new_b[c] = b[c];
				b = new_b;
			}
			b[i++] = ch;
			changed = false;
		}


		/** Adds wLen characters to the word being stemmed contained in a portion
		 * of a char[] array. This is like repeated calls of add(char ch), but
		 * faster.
		 */

		public void add(char[] w, int wLen) 
		{
			if (i+wLen >= b.Length) 
			{
				char[] new_b = new char[i+wLen+INC];
				for (int c = 0; c < i; c++)
					new_b[c] = b[c];
				b = new_b;
			}
			for (int c = 0; c < wLen; c++)
				b[i++] = w[c];
			changed = false;
		}

		/**
		 * After a word has been stemmed, it can be retrieved by ToString(),
		 * or a reference to the internal buffer can be retrieved by getResultBuffer
		 * and getResultLength (which is generally more efficient.)
		 */
		public override string ToString() 
		{
			return getTerm();
		}

		/**
		 * Returns the length of the word resulting from the stemming process.
		 */
		public int getResultLength() 
		{
			return i_end;
		}

		/**
		 * Returns a reference to a character buffer containing the results of
		 * the stemming process.  You also need to consult getResultLength()
		 * to determine the length of the result.
		 */
		public char[] getResultBuffer() 
		{
			return b;
		}

		/* cons(i) is true <=> b[i] is a consonant. */
		private bool cons(int i) 
		{
			switch (b[i]) 
			{
				case 'a': case 'e': case 'i': case 'o': case 'u': return false;
				case 'y': return (i==0) ? true : !cons(i-1);
				default: return true;
			}
		}

		/* m() measures the number of consonant sequences between 0 and j. if c is
		   a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
		   presence,

			  <c><v>       gives 0
			  <c>vc<v>     gives 1
			  <c>vcvc<v>   gives 2
			  <c>vcvcvc<v> gives 3
			  ....
		*/
		private int m() 
		{
			int n = 0;
			int i = 0;
			while(true) 
			{
				if (i > j) return n;
				if (! cons(i)) break; i++;
			}
			i++;
			while(true) 
			{
				while(true) 
				{
					if (i > j) return n;
					if (cons(i)) break;
					i++;
				}
				i++;
				n++;
				while(true) 
				{
					if (i > j) return n;
					if (! cons(i)) break;
					i++;
				}
				i++;
			}
		}

		/* vowelinstem() is true <=> 0,...j contains a vowel */
		private bool vowelinstem() 
		{
			int i;
			for (i = 0; i <= j; i++)
				if (! cons(i))
					return true;
			return false;
		}

		/* doublec(j) is true <=> j,(j-1) contain a double consonant. */
		private bool doublec(int j) 
		{
			if (j < 1)
				return false;
			if (b[j] != b[j-1])
				return false;
			return cons(j);
		}

		/* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
		   and also if the second c is not w,x or y. this is used when trying to
		   restore an e at the end of a short word. e.g.

			  cav(e), lov(e), hop(e), crim(e), but
			  snow, box, tray.

		*/
		private bool cvc(int i) 
		{
			if (i < 2 || !cons(i) || cons(i-1) || !cons(i-2))
				return false;
			int ch = b[i];
			if (ch == 'w' || ch == 'x' || ch == 'y')
				return false;
			return true;
		}

		private bool ends(String s) 
		{
			int l = s.Length;
			int o = k-l+1;
			if (o < 0)
				return false;
			char[] sc = s.ToCharArray();
			for (int i = 0; i < l; i++)
				if (b[o+i] != sc[i])
					return false;
			j = k-l;
			return true;
		}

		/* setto(s) sets (j+1),...k to the characters in the string s, readjusting
		   k. */
		private void setto(String s) 
		{
			int l = s.Length;
			int o = j+1;
			char[] sc = s.ToCharArray();
			for (int i = 0; i < l; i++)
				b[o+i] = sc[i];
			k = j+l;
			changed = true;
		}

		/* r(s) is used further down. */
		private void r(String s) 
		{
			if (m() > 0)
				setto(s);
		}

		/* step1() gets rid of plurals and -ed or -ing. e.g.
			   caresses  ->  caress
			   ponies    ->  poni
			   ties      ->  ti
			   caress    ->  caress
			   cats      ->  cat

			   feed      ->  feed
			   agreed    ->  agree
			   disabled  ->  disable

			   matting   ->  mat
			   mating    ->  mate
			   meeting   ->  meet
			   milling   ->  mill
			   messing   ->  mess

			   meetings  ->  meet

		*/

		private void step1A()
		{
			if (ends("s'"))
			{
				k--;
			}
			if (b[k] == 's')
			{
				if (ends("sses"))
					k -= 2;
				else if (ends("ies"))
					setto("y");
				else if (ends("'s"))
					k -= 2;
				else if (b[k - 1] == 'u')
				{
					if (b.Length > 3)
					{
						if (ends("ous"))
							k -= 3;
						else if (cons(k - 2) && b[k - 2] != 's' && !ends("stus"))
						{
							k--;
						}
					}
				}
				else if (b[k - 1] != 's')
					k--;
			}
		}

		private void step1B()
		{

			if (ends("eed")) 
			{
				if (m() > 0)
					k--;
			}
			else if ((ends("ed") || ends("ing")) && vowelinstem()) 
			{
				k = j;
				if (ends("at"))
					setto("ate");
				else if (ends("bl"))
					setto("ble");
				else if (ends("iz"))
					setto("ize");
				else if (doublec(k)) 
				{
					k--;
					int ch = b[k];
					if (ch == 'l' || ch == 's' || ch == 'z')
						k++;
				}
				else if (m() == 1 && cvc(k)) setto("e");
			}
		}

		/* step2() turns terminal y to i when there is another vowel in the stem, unless the
		 * immediately preceding letter is a vowel. */
		private void step2() 
		{
			if (ends("y") && (k > 0 && cons(k - 1)) && vowelinstem())
			{
				changed = true;
				b[k] = 'i';
			}
		}

		/* step3() maps double suffixes to single ones. so -ization ( = -ize plus
		   -ation) maps to -ize etc. note that the string before the suffix must give
		   m() > 0. */
		private void step3() 
		{
			if (k == 0)
				return;
			
			/* For Bug 1 */
			switch (b[k-1]) 
			{
				case 'a':
					if (ends("ational")) { r("ate"); break; }
					if (ends("tional")) { r("tion"); break; }
					break;
				case 'c':
					if (ends("enci")) { r("ence"); break; }
					if (ends("anci")) { r("ance"); break; }
					break;
				case 'e':
					if (ends("izer")) { r("ize"); break; }
					break;
				case 'l':
					if (ends("bli")) { r("ble"); break; }
					if (ends("ealli")) { setto("eal"); break; }
					if (ends("alli")) { r("al"); break; }
					if (ends("ulli")) { setto((j == 0) ? "ull" : "ul"); break; }
					if (ends("entli")) { r("ent"); break; }
					if (ends("eli") && (changed)) { r("e"); break; }
					if (ends("obeli")) { setto("obel"); break; }
					if (ends("ousli")) { r("ous"); break; }
					break;
				case 'o':
					if (ends("ization")) { r("ize"); break; }
					if (ends("ation")) { r("ate"); break; }
					if (ends("ator")) { r("ate"); break; }
					break;
				case 's':
					if (ends("alism")) { r("al"); break; }
					if (ends("iveness")) { r("ive"); break; }
					if (ends("fulness")) { r("ful"); break; }
					if (ends("ousness")) { r("ous"); break; }
					break;
				case 't':
					if (ends("aliti")) { r("al"); break; }
					if (ends("iviti")) { r("ive"); break; }
					if (ends("biliti")) { r("ble"); break; }
					break;
				case 'g':
					if (ends("logi")) { r("log"); break; }
					break;
				default :
					break;
			}
		}

		/* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
		private void step4() 
		{
			switch (b[k]) 
			{
				case 'e':
					if (ends("icate")) { r("ic"); break; }
					if (ends("ative")) { r(""); break; }
					if (ends("alize")) { r("al"); break; }
					break;
				case 'i':
					if (ends("iciti")) { r("ic"); break; }
					break;
				case 'l':
					if (ends("ical")) { r("ic"); break; }
					if (ends("ful")) { r(""); break; }
					break;
				case 's':
					if (ends("ness")) { r(""); break; }
					break;
			}
		}

		/* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
		private void step5() 
		{
			if (k == 0)
				return;

			/* for Bug 1 */
			switch ( b[k-1] ) 
			{
				case 'a':
					if (ends("al")) break; return;
				case 'c':
					if (ends("ance")) break;
					if (ends("ence")) break; return;
				case 'e':
					if (ends("er")) break; return;
				case 'i':
					if (ends("ic")) break; return;
				case 'l':
					if (ends("able")) break;
					if (ends("ible")) break;
					if (ends("ple")) break; return;
				case 'n':
					if (ends("ant")) break;
					if (ends("ement")) break;
					if (ends("ment")) break;
					/* element etc. not stripped before the m */
					if (ends("ent")) break; return;
				case 'o':
					if (ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
					/* j >= 0 fixes Bug 2 */
					if (ends("ou")) break; return;
					/* takes care of -ous */
				case 's':
					if (ends("ism")) break; return;
				case 't':
					if (ends("ate")) break;
					if (ends("iti")) break; return;
				case 'u':
					if (ends("ous")) break; return;
				case 'v':
					if (ends("ive")) break; return;
				case 'z':
					if (ends("ize")) break; return;
				default:
					return;
			}
			if (m() > 1)
				k = j;
		}

		/* step6() removes a final -e if m() > 1. */
		private void step6() 
		{
			j = k;
			
			if (b[k] == 'e') 
			{
				if (ends("ple") || ends("tle") || k < 3)
					return;
				int a = m();
				if ((a > 1 || a == 1 && !cvc(k-1)) && b[k-1] != 'e')
					k--;
			}
			if (b[k] == 'l' && doublec(k) && m() > 1)
				k--;
		}

		/** Stem the word placed into the Stemmer buffer through calls to add().
		 * Returns true if the stemming process resulted in a word different
		 * from the input.  You can retrieve the result with
		 * getResultLength()/getResultBuffer() or toString().
		 */
		public void stem() 
		{
			k = i - 1;
			if (k > 1) 
			{
				step1A();
				if (!specialCaseNoun() && !b.Contains('-'))
				{
					if ((!changed && k == i - 1) || !StagedStemming)
					{
						step1B();
						if ((!changed && k == i - 1) || !StagedStemming)
						{
							step2();
							step3();
							if (!changed || !StagedStemming)
							{
								step4();
								if (!changed || !StagedStemming)
								{
									step5();
									if ((!changed && k == i - 1) || !StagedStemming)
										step6();
								}
							}
						}
					}
				}
			}
			i_end = k+1;
			i = 0;
		}

		private bool specialCaseNoun()
		{
			return (ends("morning") || ends("olive") ||
				ends("someone") || ends("anyone") || ends("everyone") ||
				ends("something") || ends("anything") || ends("everything") || ends("nothing") ||
				ends("somebody") || ends("anybody") || ends("everybody") || ends("nobody"));
		}
	}
}
