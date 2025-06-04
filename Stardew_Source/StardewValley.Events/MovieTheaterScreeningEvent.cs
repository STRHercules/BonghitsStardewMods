using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.GameData.Movies;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;

namespace StardewValley.Events;

/// <summary>Generates the event that plays when watching a movie at the <see cref="T:StardewValley.Locations.MovieTheater" />.</summary>
public class MovieTheaterScreeningEvent
{
	public int currentResponse;

	public List<List<Character>> playerAndGuestAudienceGroups;

	public Dictionary<int, Character> _responseOrder = new Dictionary<int, Character>();

	protected Dictionary<Character, Character> _whiteListDependencyLookup;

	protected Dictionary<Character, string> _characterResponses;

	public MovieData movieData;

	protected List<Farmer> _farmers;

	protected Dictionary<Character, MovieConcession> _concessionsData;

	public Event getMovieEvent(string movieId, List<List<Character>> player_and_guest_audience_groups, List<List<Character>> npcOnlyAudienceGroups, Dictionary<Character, MovieConcession> concessions_data = null)
	{
		_concessionsData = concessions_data;
		_responseOrder = new Dictionary<int, Character>();
		_whiteListDependencyLookup = new Dictionary<Character, Character>();
		_characterResponses = new Dictionary<Character, string>();
		movieData = MovieTheater.GetMovieDataById()[movieId];
		playerAndGuestAudienceGroups = player_and_guest_audience_groups;
		currentResponse = 0;
		StringBuilder sb = new StringBuilder();
		Random theaterRandom = Utility.CreateDaySaveRandom();
		sb.Append("movieScreenAmbience/-2000 -2000/");
		string playerCharacterEventName = "farmer" + Utility.getFarmerNumberFromFarmer(Game1.player);
		string playerCharacterGuestName = "";
		bool hasPlayerGuest = false;
		foreach (List<Character> list in playerAndGuestAudienceGroups)
		{
			if (!list.Contains(Game1.player))
			{
				continue;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (!(list[i] is Farmer))
				{
					playerCharacterGuestName = list[i].name.Value;
					hasPlayerGuest = true;
					break;
				}
			}
		}
		_farmers = new List<Farmer>();
		foreach (List<Character> playerAndGuestAudienceGroup in playerAndGuestAudienceGroups)
		{
			foreach (Character item in playerAndGuestAudienceGroup)
			{
				if (item is Farmer player && !_farmers.Contains(player))
				{
					_farmers.Add(player);
				}
			}
		}
		List<Character> allAudience = playerAndGuestAudienceGroups.SelectMany((List<Character> x) => x).ToList();
		if (allAudience.Count <= 12)
		{
			allAudience.AddRange(npcOnlyAudienceGroups.SelectMany((List<Character> x) => x).ToList());
		}
		bool first = true;
		foreach (Character c in allAudience)
		{
			if (c != null)
			{
				if (!first)
				{
					sb.Append(' ');
				}
				if (c is Farmer f)
				{
					sb.Append("farmer").Append(Utility.getFarmerNumberFromFarmer(f));
				}
				else
				{
					sb.Append(c.name.Value);
				}
				sb.Append(" -1000 -1000 0");
				first = false;
			}
		}
		sb.Append("/changeToTemporaryMap MovieTheaterScreen false/specificTemporarySprite movieTheater_setup/ambientLight 0 0 0/");
		string[] backRow = new string[8];
		string[] midRow = new string[6];
		string[] frontRow = new string[4];
		playerAndGuestAudienceGroups = playerAndGuestAudienceGroups.OrderBy((List<Character> x) => theaterRandom.Next()).ToList();
		int startingSeat = theaterRandom.Next(8 - Math.Min(playerAndGuestAudienceGroups.SelectMany((List<Character> x) => x).Count(), 8) + 1);
		int whichGroup = 0;
		if (playerAndGuestAudienceGroups.Count > 0)
		{
			for (int j = 0; j < 8; j++)
			{
				int seat = (j + startingSeat) % 8;
				if (playerAndGuestAudienceGroups[whichGroup].Count == 2 && (seat == 3 || seat == 7))
				{
					j++;
					seat++;
					seat %= 8;
				}
				for (int k = 0; k < playerAndGuestAudienceGroups[whichGroup].Count && seat + k < backRow.Length; k++)
				{
					backRow[seat + k] = ((playerAndGuestAudienceGroups[whichGroup][k] is Farmer) ? ("farmer" + Utility.getFarmerNumberFromFarmer(playerAndGuestAudienceGroups[whichGroup][k] as Farmer)) : playerAndGuestAudienceGroups[whichGroup][k].name.Value);
					if (k > 0)
					{
						j++;
					}
				}
				whichGroup++;
				if (whichGroup >= playerAndGuestAudienceGroups.Count)
				{
					break;
				}
			}
		}
		else
		{
			Game1.log.Warn("The movie audience somehow has no players. This is likely a bug.");
		}
		bool usedMidRow = false;
		if (whichGroup < playerAndGuestAudienceGroups.Count)
		{
			startingSeat = 0;
			for (int l = 0; l < 4; l++)
			{
				int seat2 = (l + startingSeat) % 4;
				for (int m = 0; m < playerAndGuestAudienceGroups[whichGroup].Count && seat2 + m < frontRow.Length; m++)
				{
					frontRow[seat2 + m] = ((playerAndGuestAudienceGroups[whichGroup][m] is Farmer) ? ("farmer" + Utility.getFarmerNumberFromFarmer(playerAndGuestAudienceGroups[whichGroup][m] as Farmer)) : playerAndGuestAudienceGroups[whichGroup][m].name.Value);
					if (m > 0)
					{
						l++;
					}
				}
				whichGroup++;
				if (whichGroup >= playerAndGuestAudienceGroups.Count)
				{
					break;
				}
			}
			if (whichGroup < playerAndGuestAudienceGroups.Count)
			{
				usedMidRow = true;
				startingSeat = 0;
				for (int n = 0; n < 6; n++)
				{
					int seat3 = (n + startingSeat) % 6;
					if (playerAndGuestAudienceGroups[whichGroup].Count == 2 && seat3 == 2)
					{
						n++;
						seat3++;
						seat3 %= 8;
					}
					for (int num = 0; num < playerAndGuestAudienceGroups[whichGroup].Count && seat3 + num < midRow.Length; num++)
					{
						midRow[seat3 + num] = ((playerAndGuestAudienceGroups[whichGroup][num] is Farmer) ? ("farmer" + Utility.getFarmerNumberFromFarmer(playerAndGuestAudienceGroups[whichGroup][num] as Farmer)) : playerAndGuestAudienceGroups[whichGroup][num].name.Value);
						if (num > 0)
						{
							n++;
						}
					}
					whichGroup++;
					if (whichGroup >= playerAndGuestAudienceGroups.Count)
					{
						break;
					}
				}
			}
		}
		if (!usedMidRow)
		{
			for (int num2 = 0; num2 < npcOnlyAudienceGroups.Count; num2++)
			{
				int seat4 = theaterRandom.Next(3 - npcOnlyAudienceGroups[num2].Count + 1) + num2 * 3;
				for (int num3 = 0; num3 < npcOnlyAudienceGroups[num2].Count; num3++)
				{
					midRow[seat4 + num3] = npcOnlyAudienceGroups[num2][num3].name.Value;
				}
			}
		}
		int soFar = 0;
		int sittingTogetherCount = 0;
		for (int num4 = 0; num4 < backRow.Length; num4++)
		{
			if (string.IsNullOrEmpty(backRow[num4]) || !(backRow[num4] != playerCharacterEventName) || !(backRow[num4] != playerCharacterGuestName))
			{
				continue;
			}
			soFar++;
			if (soFar < 2)
			{
				continue;
			}
			sittingTogetherCount++;
			Point seat5 = getBackRowSeatTileFromIndex(num4);
			sb.Append("warp ").Append(backRow[num4]).Append(' ')
				.Append(seat5.X)
				.Append(' ')
				.Append(seat5.Y)
				.Append("/positionOffset ")
				.Append(backRow[num4])
				.Append(" 0 -10/");
			if (sittingTogetherCount == 2)
			{
				sittingTogetherCount = 0;
				if (theaterRandom.NextBool() && backRow[num4] != playerCharacterGuestName && backRow[num4 - 1] != playerCharacterGuestName && backRow[num4 - 1] != null)
				{
					sb.Append("faceDirection ").Append(backRow[num4]).Append(" 3 true/");
					sb.Append("faceDirection ").Append(backRow[num4 - 1]).Append(" 1 true/");
				}
			}
		}
		soFar = 0;
		sittingTogetherCount = 0;
		for (int num5 = 0; num5 < midRow.Length; num5++)
		{
			if (string.IsNullOrEmpty(midRow[num5]) || !(midRow[num5] != playerCharacterEventName) || !(midRow[num5] != playerCharacterGuestName))
			{
				continue;
			}
			soFar++;
			if (soFar < 2)
			{
				continue;
			}
			sittingTogetherCount++;
			Point seat6 = getMidRowSeatTileFromIndex(num5);
			sb.Append("warp ").Append(midRow[num5]).Append(' ')
				.Append(seat6.X)
				.Append(' ')
				.Append(seat6.Y)
				.Append("/positionOffset ")
				.Append(midRow[num5])
				.Append(" 0 -10/");
			if (sittingTogetherCount == 2)
			{
				sittingTogetherCount = 0;
				if (num5 != 3 && theaterRandom.NextBool() && midRow[num5 - 1] != null)
				{
					sb.Append("faceDirection ").Append(midRow[num5]).Append(" 3 true/");
					sb.Append("faceDirection ").Append(midRow[num5 - 1]).Append(" 1 true/");
				}
			}
		}
		soFar = 0;
		sittingTogetherCount = 0;
		for (int num6 = 0; num6 < frontRow.Length; num6++)
		{
			if (string.IsNullOrEmpty(frontRow[num6]) || !(frontRow[num6] != playerCharacterEventName) || !(frontRow[num6] != playerCharacterGuestName))
			{
				continue;
			}
			soFar++;
			if (soFar < 2)
			{
				continue;
			}
			sittingTogetherCount++;
			Point seat7 = getFrontRowSeatTileFromIndex(num6);
			sb.Append("warp ").Append(frontRow[num6]).Append(' ')
				.Append(seat7.X)
				.Append(' ')
				.Append(seat7.Y)
				.Append("/positionOffset ")
				.Append(frontRow[num6])
				.Append(" 0 -10/");
			if (sittingTogetherCount == 2)
			{
				sittingTogetherCount = 0;
				if (theaterRandom.NextBool() && frontRow[num6 - 1] != null)
				{
					sb.Append("faceDirection ").Append(frontRow[num6]).Append(" 3 true/");
					sb.Append("faceDirection ").Append(frontRow[num6 - 1]).Append(" 1 true/");
				}
			}
		}
		Point warpPoint = new Point(1, 15);
		soFar = 0;
		for (int num7 = 0; num7 < backRow.Length; num7++)
		{
			if (!string.IsNullOrEmpty(backRow[num7]) && backRow[num7] != playerCharacterEventName && backRow[num7] != playerCharacterGuestName)
			{
				Point seat8 = getBackRowSeatTileFromIndex(num7);
				if (soFar == 1)
				{
					sb.Append("warp ").Append(backRow[num7]).Append(' ')
						.Append(seat8.X - 1)
						.Append(" 10")
						.Append("/advancedMove ")
						.Append(backRow[num7])
						.Append(" false 1 ")
						.Append(200)
						.Append(" 1 0 4 1000/")
						.Append("positionOffset ")
						.Append(backRow[num7])
						.Append(" 0 -10/");
				}
				else
				{
					sb.Append("warp ").Append(backRow[num7]).Append(" 1 12")
						.Append("/advancedMove ")
						.Append(backRow[num7])
						.Append(" false 1 200 ")
						.Append("0 -2 ")
						.Append(seat8.X - 1)
						.Append(" 0 4 1000/")
						.Append("positionOffset ")
						.Append(backRow[num7])
						.Append(" 0 -10/");
				}
				soFar++;
			}
			if (soFar >= 2)
			{
				break;
			}
		}
		soFar = 0;
		for (int num8 = 0; num8 < midRow.Length; num8++)
		{
			if (!string.IsNullOrEmpty(midRow[num8]) && midRow[num8] != playerCharacterEventName && midRow[num8] != playerCharacterGuestName)
			{
				Point seat9 = getMidRowSeatTileFromIndex(num8);
				if (soFar == 1)
				{
					sb.Append("warp ").Append(midRow[num8]).Append(' ')
						.Append(seat9.X - 1)
						.Append(" 8")
						.Append("/advancedMove ")
						.Append(midRow[num8])
						.Append(" false 1 ")
						.Append(400)
						.Append(" 1 0 4 1000/");
				}
				else
				{
					sb.Append("warp ").Append(midRow[num8]).Append(" 2 9")
						.Append("/advancedMove ")
						.Append(midRow[num8])
						.Append(" false 1 300 ")
						.Append("0 -1 ")
						.Append(seat9.X - 2)
						.Append(" 0 4 1000/");
				}
				soFar++;
			}
			if (soFar >= 2)
			{
				break;
			}
		}
		soFar = 0;
		for (int num9 = 0; num9 < frontRow.Length; num9++)
		{
			if (!string.IsNullOrEmpty(frontRow[num9]) && frontRow[num9] != playerCharacterEventName && frontRow[num9] != playerCharacterGuestName)
			{
				Point seat10 = getFrontRowSeatTileFromIndex(num9);
				if (soFar == 1)
				{
					sb.Append("warp ").Append(frontRow[num9]).Append(' ')
						.Append(seat10.X - 1)
						.Append(" 6")
						.Append("/advancedMove ")
						.Append(frontRow[num9])
						.Append(" false 1 ")
						.Append(400)
						.Append(" 1 0 4 1000/");
				}
				else
				{
					sb.Append("warp ").Append(frontRow[num9]).Append(" 3 7")
						.Append("/advancedMove ")
						.Append(frontRow[num9])
						.Append(" false 1 300 ")
						.Append("0 -1 ")
						.Append(seat10.X - 3)
						.Append(" 0 4 1000/");
				}
				soFar++;
			}
			if (soFar >= 2)
			{
				break;
			}
		}
		sb.Append("viewport 6 8 true/pause 500/");
		for (int num10 = 0; num10 < backRow.Length; num10++)
		{
			if (!string.IsNullOrEmpty(backRow[num10]))
			{
				Point seat11 = getBackRowSeatTileFromIndex(num10);
				if (backRow[num10] == playerCharacterEventName || backRow[num10] == playerCharacterGuestName)
				{
					sb.Append("warp ").Append(backRow[num10]).Append(' ')
						.Append(warpPoint.X)
						.Append(' ')
						.Append(warpPoint.Y)
						.Append("/advancedMove ")
						.Append(backRow[num10])
						.Append(" false 0 -5 ")
						.Append(seat11.X - warpPoint.X)
						.Append(" 0 4 1000/")
						.Append("pause ")
						.Append(1000)
						.Append("/");
				}
			}
		}
		for (int num11 = 0; num11 < midRow.Length; num11++)
		{
			if (!string.IsNullOrEmpty(midRow[num11]))
			{
				Point seat12 = getMidRowSeatTileFromIndex(num11);
				if (midRow[num11] == playerCharacterEventName || midRow[num11] == playerCharacterGuestName)
				{
					sb.Append("warp ").Append(midRow[num11]).Append(' ')
						.Append(warpPoint.X)
						.Append(' ')
						.Append(warpPoint.Y)
						.Append("/advancedMove ")
						.Append(midRow[num11])
						.Append(" false 0 -7 ")
						.Append(seat12.X - warpPoint.X)
						.Append(" 0 4 1000/")
						.Append("pause ")
						.Append(1000)
						.Append("/");
				}
			}
		}
		for (int num12 = 0; num12 < frontRow.Length; num12++)
		{
			if (!string.IsNullOrEmpty(frontRow[num12]))
			{
				Point seat13 = getFrontRowSeatTileFromIndex(num12);
				if (frontRow[num12] == playerCharacterEventName || frontRow[num12] == playerCharacterGuestName)
				{
					sb.Append("warp ").Append(frontRow[num12]).Append(' ')
						.Append(warpPoint.X)
						.Append(' ')
						.Append(warpPoint.Y)
						.Append("/advancedMove ")
						.Append(frontRow[num12])
						.Append(" false 0 -7 1 0 0 -1 1 0 0 -1 ")
						.Append(seat13.X - 3)
						.Append(" 0 4 1000/")
						.Append("pause ")
						.Append(1000)
						.Append("/");
				}
			}
		}
		sb.Append("pause 3000");
		if (hasPlayerGuest)
		{
			sb.Append("/proceedPosition ").Append(playerCharacterGuestName);
		}
		sb.Append("/pause 1000");
		if (!hasPlayerGuest)
		{
			sb.Append("/proceedPosition farmer");
		}
		sb.Append("/waitForAllStationary/pause 100");
		foreach (Character c2 in allAudience)
		{
			string actorName = getEventName(c2);
			if (actorName != playerCharacterEventName && actorName != playerCharacterGuestName)
			{
				if (c2 is Farmer)
				{
					sb.Append("/faceDirection ").Append(actorName).Append(" 0 true/positionOffset ")
						.Append(actorName)
						.Append(" 0 42 true");
				}
				else
				{
					sb.Append("/faceDirection ").Append(actorName).Append(" 0 true/positionOffset ")
						.Append(actorName)
						.Append(" 0 12 true");
				}
				if (theaterRandom.NextDouble() < 0.2)
				{
					sb.Append("/pause 100");
				}
			}
		}
		sb.Append("/positionOffset ").Append(playerCharacterEventName).Append(" 0 32");
		if (hasPlayerGuest)
		{
			sb.Append("/positionOffset ").Append(playerCharacterGuestName).Append(" 0 8");
		}
		sb.Append("/ambientLight 210 210 120 true/pause 500/viewport move 0 -1 4000/pause 5000");
		List<Character> responding_characters = new List<Character>();
		foreach (List<Character> playerAndGuestAudienceGroup2 in playerAndGuestAudienceGroups)
		{
			foreach (Character character in playerAndGuestAudienceGroup2)
			{
				if (!(character is Farmer) && !responding_characters.Contains(character))
				{
					responding_characters.Add(character);
				}
			}
		}
		for (int num13 = 0; num13 < responding_characters.Count; num13++)
		{
			int index = theaterRandom.Next(responding_characters.Count);
			Character character2 = responding_characters[num13];
			responding_characters[num13] = responding_characters[index];
			responding_characters[index] = character2;
		}
		int current_response_index = 0;
		foreach (MovieScene scene in movieData.Scenes)
		{
			if (scene.ResponsePoint == null)
			{
				continue;
			}
			bool found_reaction = false;
			for (int num14 = 0; num14 < responding_characters.Count; num14++)
			{
				MovieCharacterReaction reaction = MovieTheater.GetReactionsForCharacter(responding_characters[num14] as NPC);
				if (reaction == null)
				{
					continue;
				}
				foreach (MovieReaction movie_reaction in reaction.Reactions)
				{
					if (!movie_reaction.ShouldApplyToMovie(movieData, MovieTheater.GetPatronNames(), MovieTheater.GetResponseForMovie(responding_characters[num14] as NPC)) || movie_reaction.SpecialResponses?.DuringMovie == null || (!(movie_reaction.SpecialResponses.DuringMovie.ResponsePoint == scene.ResponsePoint) && movie_reaction.Whitelist.Count <= 0))
					{
						continue;
					}
					if (!_whiteListDependencyLookup.ContainsKey(responding_characters[num14]))
					{
						_responseOrder[current_response_index] = responding_characters[num14];
						if (movie_reaction.Whitelist != null)
						{
							for (int num15 = 0; num15 < movie_reaction.Whitelist.Count; num15++)
							{
								Character white_list_character = Game1.getCharacterFromName(movie_reaction.Whitelist[num15]);
								if (white_list_character == null)
								{
									continue;
								}
								_whiteListDependencyLookup[white_list_character] = responding_characters[num14];
								foreach (int key in _responseOrder.Keys)
								{
									if (_responseOrder[key] == white_list_character)
									{
										_responseOrder.Remove(key);
									}
								}
							}
						}
					}
					responding_characters.RemoveAt(num14);
					num14--;
					found_reaction = true;
					break;
				}
				if (found_reaction)
				{
					break;
				}
			}
			if (!found_reaction)
			{
				for (int num16 = 0; num16 < responding_characters.Count; num16++)
				{
					MovieCharacterReaction reaction2 = MovieTheater.GetReactionsForCharacter(responding_characters[num16] as NPC);
					if (reaction2 == null)
					{
						continue;
					}
					foreach (MovieReaction movie_reaction2 in reaction2.Reactions)
					{
						if (!movie_reaction2.ShouldApplyToMovie(movieData, MovieTheater.GetPatronNames(), MovieTheater.GetResponseForMovie(responding_characters[num16] as NPC)) || movie_reaction2.SpecialResponses?.DuringMovie == null || !(movie_reaction2.SpecialResponses.DuringMovie.ResponsePoint == current_response_index.ToString()))
						{
							continue;
						}
						if (!_whiteListDependencyLookup.ContainsKey(responding_characters[num16]))
						{
							_responseOrder[current_response_index] = responding_characters[num16];
							if (movie_reaction2.Whitelist != null)
							{
								for (int num17 = 0; num17 < movie_reaction2.Whitelist.Count; num17++)
								{
									Character white_list_character2 = Game1.getCharacterFromName(movie_reaction2.Whitelist[num17]);
									if (white_list_character2 == null)
									{
										continue;
									}
									_whiteListDependencyLookup[white_list_character2] = responding_characters[num16];
									foreach (int key2 in _responseOrder.Keys)
									{
										if (_responseOrder[key2] == white_list_character2)
										{
											_responseOrder.Remove(key2);
										}
									}
								}
							}
						}
						responding_characters.RemoveAt(num16);
						num16--;
						found_reaction = true;
						break;
					}
					if (found_reaction)
					{
						break;
					}
				}
			}
			current_response_index++;
		}
		current_response_index = 0;
		for (int num18 = 0; num18 < responding_characters.Count; num18++)
		{
			if (!_whiteListDependencyLookup.ContainsKey(responding_characters[num18]))
			{
				for (; _responseOrder.ContainsKey(current_response_index); current_response_index++)
				{
				}
				_responseOrder[current_response_index] = responding_characters[num18];
				current_response_index++;
			}
		}
		responding_characters = null;
		foreach (MovieScene scene2 in movieData.Scenes)
		{
			_ParseScene(sb, scene2);
		}
		while (currentResponse < _responseOrder.Count)
		{
			_ParseResponse(sb);
		}
		sb.Append("/stopMusic");
		sb.Append("/fade/viewport -1000 -1000");
		sb.Append("/pause 500/message \"").Append(Game1.content.LoadString("Strings\\Locations:Theater_MovieEnd")).Append("\"/pause 500");
		sb.Append("/requestMovieEnd");
		return new Event(sb.ToString(), null, "MovieTheaterScreening");
	}

	protected void _ParseScene(StringBuilder sb, MovieScene scene)
	{
		if (!string.IsNullOrWhiteSpace(scene.Sound))
		{
			sb.Append("/playSound ").Append(scene.Sound);
		}
		if (!string.IsNullOrWhiteSpace(scene.Music))
		{
			sb.Append("/playMusic ").Append(scene.Music);
		}
		if (scene.MessageDelay > 0)
		{
			sb.Append("/pause ").Append(scene.MessageDelay);
		}
		if (scene.Image >= 0)
		{
			sb.Append("/specificTemporarySprite movieTheater_screen ").Append(movieData.Id).Append(' ')
				.Append(scene.Image)
				.Append(' ')
				.Append(scene.Shake);
			if (movieData.Texture != null)
			{
				sb.Append(" \"").Append(ArgUtility.EscapeQuotes(movieData.Texture)).Append('"');
			}
		}
		if (!string.IsNullOrWhiteSpace(scene.Script))
		{
			sb.Append(TokenParser.ParseText(scene.Script));
		}
		if (!string.IsNullOrWhiteSpace(scene.Text))
		{
			sb.Append("/message \"").Append(ArgUtility.EscapeQuotes(TokenParser.ParseText(scene.Text))).Append('"');
		}
		if (scene.ResponsePoint != null)
		{
			_ParseResponse(sb, scene);
		}
	}

	protected void _ParseResponse(StringBuilder sb, MovieScene scene = null)
	{
		if (_responseOrder.TryGetValue(currentResponse, out var responding_character))
		{
			sb.Append("/pause 500");
			bool hadUniqueScript = false;
			if (!_whiteListDependencyLookup.ContainsKey(responding_character))
			{
				MovieCharacterReaction reaction = MovieTheater.GetReactionsForCharacter(responding_character as NPC);
				if (reaction != null)
				{
					foreach (MovieReaction movie_reaction in reaction.Reactions)
					{
						if (movie_reaction.ShouldApplyToMovie(movieData, MovieTheater.GetPatronNames(), MovieTheater.GetResponseForMovie(responding_character as NPC)) && movie_reaction.SpecialResponses?.DuringMovie != null && (string.IsNullOrEmpty(movie_reaction.SpecialResponses.DuringMovie.ResponsePoint) || (scene != null && movie_reaction.SpecialResponses.DuringMovie.ResponsePoint == scene.ResponsePoint) || movie_reaction.SpecialResponses.DuringMovie.ResponsePoint == currentResponse.ToString() || movie_reaction.Whitelist.Count > 0))
						{
							string script = TokenParser.ParseText(movie_reaction.SpecialResponses.DuringMovie.Script);
							string text = TokenParser.ParseText(movie_reaction.SpecialResponses.DuringMovie.Text);
							if (!string.IsNullOrWhiteSpace(script))
							{
								sb.Append(script);
								hadUniqueScript = true;
							}
							if (!string.IsNullOrWhiteSpace(text))
							{
								sb.Append("/speak ").Append(responding_character.name.Value).Append(" \"")
									.Append(text)
									.Append('"');
							}
							break;
						}
					}
				}
			}
			_ParseCharacterResponse(sb, responding_character, hadUniqueScript);
			foreach (Character key in _whiteListDependencyLookup.Keys)
			{
				if (_whiteListDependencyLookup[key] == responding_character)
				{
					_ParseCharacterResponse(sb, key);
				}
			}
		}
		currentResponse++;
	}

	protected void _ParseCharacterResponse(StringBuilder sb, Character responding_character, bool ignoreScript = false)
	{
		string response = MovieTheater.GetResponseForMovie(responding_character as NPC);
		if (_whiteListDependencyLookup.TryGetValue(responding_character, out var requestingCharacter))
		{
			response = MovieTheater.GetResponseForMovie(requestingCharacter as NPC);
		}
		switch (response)
		{
		case "love":
			sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
				.Append(200);
			if (!ignoreScript)
			{
				sb.Append("/playSound reward/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(20)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LoveMovie", responding_character.displayName))
					.Append('"');
			}
			break;
		case "like":
			sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
				.Append(100);
			if (!ignoreScript)
			{
				sb.Append("/playSound give_gift/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(56)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LikeMovie", responding_character.displayName))
					.Append('"');
			}
			break;
		case "dislike":
			sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
				.Append(0);
			if (!ignoreScript)
			{
				sb.Append("/playSound newArtifact/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(24)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_DislikeMovie", responding_character.displayName))
					.Append('"');
			}
			break;
		}
		if (_concessionsData != null && _concessionsData.TryGetValue(responding_character, out var concession))
		{
			string concession_response = MovieTheater.GetConcessionTasteForCharacter(responding_character, concession);
			string gender_tag = "";
			if (NPC.TryGetData(responding_character.name.Value, out var npcData))
			{
				switch (npcData.Gender)
				{
				case Gender.Female:
					gender_tag = "_Female";
					break;
				case Gender.Male:
					gender_tag = "_Male";
					break;
				}
			}
			string sound = "eat";
			if (concession.Tags != null && concession.Tags.Contains("Drink"))
			{
				sound = "gulp";
			}
			switch (concession_response)
			{
			case "love":
				sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
					.Append(50);
				sb.Append("/tossConcession ").Append(responding_character.Name).Append(' ')
					.Append(concession.Id)
					.Append("/pause 1000");
				sb.Append("/playSound ").Append(sound).Append("/shake ")
					.Append(responding_character.Name)
					.Append(" 500/pause 1000");
				sb.Append("/playSound reward/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(20)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LoveConcession" + gender_tag, responding_character.displayName, concession.DisplayName))
					.Append('"');
				break;
			case "like":
				sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
					.Append(25);
				sb.Append("/tossConcession ").Append(responding_character.Name).Append(' ')
					.Append(concession.Id)
					.Append("/pause 1000");
				sb.Append("/playSound ").Append(sound).Append("/shake ")
					.Append(responding_character.Name)
					.Append(" 500/pause 1000");
				sb.Append("/playSound give_gift/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(56)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LikeConcession" + gender_tag, responding_character.displayName, concession.DisplayName))
					.Append('"');
				break;
			case "dislike":
				sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
					.Append(0);
				sb.Append("/playSound croak/pause 1000");
				sb.Append("/playSound newArtifact/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(40)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_DislikeConcession" + gender_tag, responding_character.displayName, concession.DisplayName))
					.Append('"');
				break;
			}
		}
		_characterResponses[responding_character] = response;
	}

	public Dictionary<Character, string> GetCharacterResponses()
	{
		return _characterResponses;
	}

	private static string getEventName(Character c)
	{
		if (c is Farmer player)
		{
			return "farmer" + Utility.getFarmerNumberFromFarmer(player);
		}
		return c.name.Value;
	}

	private Point getBackRowSeatTileFromIndex(int index)
	{
		return index switch
		{
			0 => new Point(2, 10), 
			1 => new Point(3, 10), 
			2 => new Point(4, 10), 
			3 => new Point(5, 10), 
			4 => new Point(8, 10), 
			5 => new Point(9, 10), 
			6 => new Point(10, 10), 
			7 => new Point(11, 10), 
			_ => new Point(4, 12), 
		};
	}

	private Point getMidRowSeatTileFromIndex(int index)
	{
		return index switch
		{
			0 => new Point(3, 8), 
			1 => new Point(4, 8), 
			2 => new Point(5, 8), 
			3 => new Point(8, 8), 
			4 => new Point(9, 8), 
			5 => new Point(10, 8), 
			_ => new Point(4, 12), 
		};
	}

	private Point getFrontRowSeatTileFromIndex(int index)
	{
		return index switch
		{
			0 => new Point(4, 6), 
			1 => new Point(5, 6), 
			2 => new Point(8, 6), 
			3 => new Point(9, 6), 
			_ => new Point(4, 12), 
		};
	}
}
