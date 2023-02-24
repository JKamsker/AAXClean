﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AAXClean.Test
{
	[TestClass]
	public class BA_AaxTest : AaxTestBase
	{
		private ChapterInfo _chapters;
		private TestBookTags _tags;

		public override string AaxFile => TestFiles.BA_BookPath;
		public override int AudioChannels => 2;
		public override int AudioSampleSize => 16;
		public override int TimeScale => 22050;
		public override int AverageBitrate => 64004;
		public override int MaxBitrate => 64004;
		public override long RenderSize => 5529703L;
		public override TimeSpan Duration => TimeSpan.FromTicks(581463887528);
		public override string SingleM4bHash => "f1dedd2b3e3b238d9e6c5e7c7a367c9976e39c19";
		public override List<string> MultiM4bHashes => new()
		{
"bd26999e88d62664e9d68ba587c9ce5f780c62ca",
"84a31262a77e1f639d1e61fde0fd38fec19e54b8",
"f30007c9e2b7d80283c9b3fec51f299847f22597",
"edb8081b95af1ea69512c0183b0b86967ab22459",
"b7e53897003c9028ac8ee783f955a125c8a76035",
"645f4a18c63b1985499829ba48b8d51cf5a55dd8",
"791eeb56c5d002bf52097e5bab33f30e7acb4879",
"68ec342b5043f0844cd693a8e10dea45ebab71de",
"5b89af259721f9ee5bc1a182560d544b2240a35a",
"731653cd4490b5158a134eba3b359fa00cc5f680",
"2844f4875dbe678fa954b6760fb52c734150c490",
"238ff90d95d6a251d48a13be5ee19721ef36cc13",
"d3da615fa50a2b63a015c61308e2198de45c365d",
"9a59a80250a8fb71ed47720f4b546257505ba7e3",
"b41a0191b2ddaf7996e1dba6f770f806f001a718",
"f90a724e94c13c986702247b7e949637926e949a",
"c3865387e74d8496af680861373ba092b5e38ad9",
"4bfb0b439c13b25d2d2dfd14ca0685e400ba13bc",
"7c93eddc1a0da510145e6c3c07861471b1011359",
"8ae23d1040a6c3a0f8f443e62e1deee235bac65e",
"062b6e180e1d6b64e46ee596803c21c7c40c2e73",
"185c6c429e043a62b413a94b26b47c0ba1d9fe15",
"83d9278eccfeeea5da2a126e7986375799d770d7",
"dde96a67fb3f0917ee9e5058a9fdbe9fedffb155",
"c430fbfbf30eaf62060487e119701cbdd645880d",
"6699c935c195e50fb415cd75199b1a9620cf78cc",
"9dd614a8f18bad5259a822d3dde7cbc214ad89ca",
"a2ab846acd8d5fd9334b908513868c6b027bf1e7",
"09ae0b01dd6212190266d4cb7c39c98de57e0090",
"18c9eea3ae2e8ab48ba71de15b7c77f78f207ce1",
"4f42ed23c4cf2f7cb26e8aa8ca2fe63b06f680be",
"b2b3b305b736884709e5c35933307fb1d6252c20",
"e5c313d76841387f13e3fab7e0c7be760f32a26a",
"5da309bcd3142cae1658827c046411bc9c64647c",
"84a90e17d309326d6fa10475afc8f96cde8612e9",
"4bcda51c95308439546d44638cc93f3358bfe4d0",
"63b785013cfb6b86d33f1bace7505283f001d16e",
"5b69347409ad051c5a2690333071ef597cb00e4b",
"cdb256e3ca46b67f2089b76e5d3fe7decb0a2c16",
"f141cdcbb938dccf91586b88cbdb318bdf29dcc8",
"6aa1a9ec50a2331c3d42665954377dd8b53577ed",
"b77bcf448172a3c46dab2f29305e67f483f812c6",
"9e7d7f43afed000a5ebc08c371f2b1c0cfb282bc",
"a6ae0fe86068399acca98125c8f056adf8b2ac2b",
"8f58f2788caaf0914eb3ed8c5e1877a34f054167",
"3f6e8634362e32b2c306bc20c2b9135efd619e43",
"0b3b0bda59f8071e49bdec5df50b269079b6fc79",
"7c704e206fef39b8027a6b11939100b44c195ae5",
"2862ea8e0730ac716c60bdf80c3f8f72b71c8831",
};
		public override ChapterInfo Chapters
		{
			get
			{
				if (_chapters is null)
				{
					_chapters = new ChapterInfo()
					{
						{ "Opening Credits", TimeSpan.FromTicks(1789039909) },
						{ "Chapter Four", TimeSpan.FromTicks(0) },
						{ "Part One - Injured Parties", TimeSpan.FromTicks(30000000) },
						{ "Chapter One", TimeSpan.FromTicks(8928000000) },
						{ "Chapter Two", TimeSpan.FromTicks(13649000000) },
						{ "Chapter Three", TimeSpan.FromTicks(21807489795) },
						{ "Chapter Five", TimeSpan.FromTicks(14563000000) },
						{ "Chapter Six", TimeSpan.FromTicks(20003150113) },
						{ "Chapter Seven", TimeSpan.FromTicks(14130000000) },
						{ "Chapter Eight", TimeSpan.FromTicks(16952229931) },
						{ "Part Two - Commercial Considerations", TimeSpan.FromTicks(350400000) },
						{ "Chapter Nine", TimeSpan.FromTicks(8978600000) },
						{ "Chapter Ten", TimeSpan.FromTicks(16626000000) },
						{ "Chapter Eleven", TimeSpan.FromTicks(8229879818) },
						{ "Chapter Twelve", TimeSpan.FromTicks(13901000000) },
						{ "Chapter Thirteen", TimeSpan.FromTicks(10049000000) },
						{ "Chapter Fourteen", TimeSpan.FromTicks(12620040362) },
						{ "Chapter Fifteen", TimeSpan.FromTicks(17980000000) },
						{ "Chapter Sixteen", TimeSpan.FromTicks(10099000000) },
						{ "Chapter Seventeen", TimeSpan.FromTicks(7695519727) },
						{ "Part Three - Disruptive Elements", TimeSpan.FromTicks(264070294) },
						{ "Chapter Eighteen", TimeSpan.FromTicks(8715929705) },
						{ "Chapter Nineteen", TimeSpan.FromTicks(10213000000) },
						{ "Chapter Twenty", TimeSpan.FromTicks(16245760090) },
						{ "Chapter Twenty-One", TimeSpan.FromTicks(13961000000) },
						{ "Chapter Twenty-Two", TimeSpan.FromTicks(10833000000) },
						{ "Chapter Twenty-Three", TimeSpan.FromTicks(13009479818) },
						{ "Chapter Twenty-Four", TimeSpan.FromTicks(7869000000) },
						{ "Chapter Twenty-Five", TimeSpan.FromTicks(25449310204) },
						{ "Chapter Twenty-Six", TimeSpan.FromTicks(11805000000) },
						{ "Chapter Twenty-Seven", TimeSpan.FromTicks(7354000000) },
						{ "Chapter Twenty-Eight", TimeSpan.FromTicks(8849000000) },
						{ "Chapter Twenty-Nine", TimeSpan.FromTicks(9322259863) },
						{ "Part Four - Unexplained Phenomena", TimeSpan.FromTicks(236090249) },
						{ "Chapter Thirty", TimeSpan.FromTicks(17082909750) },
						{ "Chapter Thirty-One", TimeSpan.FromTicks(15647000000) },
						{ "Chapter Thirty-Two", TimeSpan.FromTicks(15369120181) },
						{ "Chapter Thirty-Three", TimeSpan.FromTicks(16369000000) },
						{ "Chapter Thirty-Four", TimeSpan.FromTicks(18188000000) },
						{ "Chapter Thirty-Five", TimeSpan.FromTicks(4922029931) },
						{ "Part Five - Divided Loyalties", TimeSpan.FromTicks(30000000) },
						{ "Chapter Thirty-Six", TimeSpan.FromTicks(11143000000) },
						{ "Chapter Thirty-Seven", TimeSpan.FromTicks(14985000000) },
						{ "Chapter Thirty-Eight", TimeSpan.FromTicks(24184720181) },
						{ "Chapter Thirty-Nine", TimeSpan.FromTicks(10161000000) },
						{ "Chapter Forty", TimeSpan.FromTicks(11759000000) },
						{ "Chapter Forty-One", TimeSpan.FromTicks(23966349659) },
						{ "Chapter Forty-Two", TimeSpan.FromTicks(28971000000) },
						{ "Epilogue", TimeSpan.FromTicks(6177507936) },
					};
				}
				return _chapters;
			}
		}
		public override TestBookTags Tags
		{
			get
			{
				_tags ??= new TestBookTags
				{
					Album = "Broken Angels (Unabridged)",
					AlbumArtists = "Richard K. Morgan",
					Asin = "B002V8H59I",
					Comment = "Cynical, quick-on-the-trigger Takeshi Kovacs, the ex-U.N. envoy turned private eye, has changed careers, and bodies, once more....",
					Copyright = "&#169;2003  Richard K. Morgan;(P)2005  Tantor Media, Inc.",
					Generes = "Audiobook",
					LongDescription = "Cynical, quick-on-the-trigger Takeshi Kovacs, the ex-U.N. envoy turned private eye, has changed careers, and bodies, once more, trading sleuthing for soldiering as a warrior-for-hire and helping a far-flung planet's government put down a bloody revolution. \n But when it comes to taking sides, the only one Kovacs is ever really on is his own. So when a rogue pilot and a sleazy corporate fat cat offer him a lucrative role in a treacherous treasure hunt, he's only too happy to go AWOL with a band of resurrected soldiers of fortune. All that stands between them and the ancient alien spacecraft they mean to salvage are a massacred city bathed in deadly radiation, unleashed nanotechnolgy with a million ways to kill, and whatever surprises the highly advanced Martian race may have in store. But armed with his genetically engineered instincts, and his trusty twin Kalashnikovs, Takeshi is ready to take on anything...and let the devil take whoever's left behind. ",
					Narrator = "Todd McLaren",
					Performers = "Richard K. Morgan",
					ProductID = "BK_TANT_000116",
					Publisher = "Tantor Audio",
					ReleaseDate = "17-Jun-2005",
					Title = "Broken Angels (Unabridged)",
					Year = "2005",
					CoverHash = "712719a5a29fb8a3531ead6a280ce8e2c35f0f65"
				};

				return _tags;
			}
		}
	}
}
