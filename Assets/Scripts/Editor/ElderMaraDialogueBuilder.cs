#if UNITY_EDITOR
using UnityEngine;

public static class ElderMaraDialogueBuilder
{
    public static DialogueAsset CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<DialogueAsset>();
        asset.npcDisplayName = "Elder Mara";
        asset.stages = new[]
        {
            BuildFirstMeeting(),
            BuildSecondMeeting(),
            BuildTrustedAlly()
        };
        return asset;
    }

    static DialogueStage BuildFirstMeeting()
    {
        return new DialogueStage
        {
            stageName = "First Meeting",
            stageDescription = "The player discovers Elder Mara at the edge of the ruins.",
            nodes = new[]
            {
                Npc("Traveler...? Is someone truly out there? I haven't heard footsteps that weren't wrong in weeks."),
                Npc("Forgive an old woman for staring. These ruins used to be a trading road. Now the only things that travel it hunt."),
                Npc("You carry no banner, no escort, and no fear I can see on your face. That either makes you brave or terribly lost."),
                Choice(new[]
                {
                    Choice("I'm looking for a way through the ruins.",
                        "Through? There are ways, yes. None of them kind.",
                        "The old path dips behind broken stone and rises near the eastern gate.",
                        "Stay low when you cross the open stretch. The thing that patrols here sees movement before it hears it."),
                    Choice("Who are you?",
                        "Name's Mara. I was a scribe once, before the collapse.",
                        "I catalogued routes, names, debts, and safe houses. Now I catalog what still breathes.",
                        "You're the first person in a long time who looked me in the eye instead of the horizon."),
                    Choice("Is it safe here?",
                        "Safe is a word for places with walls that still mean something.",
                        "This spot gives you cover if you're quiet. The wall to the west breaks line of sight.",
                        "If it sees you, don't run in a straight line. Break sight, then move.")
                }),
                Npc("Listen carefully. The creature follows a route between the old patrol markers. I've watched it long enough to know its habits."),
                Npc("It returns to where it began if it loses you. Not because it's merciful. Because it's bound to that pattern."),
                Choice(new[]
                {
                    Choice("Can you tell me more about the creature?",
                        "Tall as two men, shoulders like a battering ram, and patient in the worst way.",
                        "It doesn't rage. It searches. That's worse, in my opinion.",
                        "If it corners you, don't waste breath begging. Save it for running."),
                    Choice("Why do you stay here?",
                        "Someone has to remember who lived here.",
                        "I stay because leaving would mean all these names become nothing but wind.",
                        "And because warnings only matter if someone is left to speak them."),
                    Choice("Do you need help with anything?",
                        "Help? From a stranger who arrived with dust on their boots?",
                        "Mara smiles, tired and genuine.",
                        "Survive the night. That would help me more than you know.")
                }),
                Npc("Take this counsel before you go: if you need to speak with me again, come when the patrol is on the far side of the stones."),
                Npc("And if you hear metal scraping on stone, don't investigate. Walk away. Then run when you're out of sight."),
                Npc("Go carefully, traveler. The ruins remember every mistake.")
            }
        };
    }

    static DialogueStage BuildSecondMeeting()
    {
        return new DialogueStage
        {
            stageName = "Second Meeting",
            stageDescription = "The player returns after surviving their first encounter with the ruins.",
            nodes = new[]
            {
                Npc("You came back. Good. I half expected the ruins to keep you."),
                Npc("Your boots are cleaner than they should be if you'd been running all night. That means you listened, or you got lucky."),
                Choice(new[]
                {
                    Choice("I hid behind the wall when it chased me.",
                        "Then you learned the most important lesson here.",
                        "Breaking line of sight isn't cowardice. It's craft.",
                        "The patrol will always return home if it can't find you. Use that."),
                    Choice("I tried to fight it.",
                        "Mara's expression hardens.",
                        "Don't do that again unless you have steel, friends, and a reason worth dying for.",
                        "That thing isn't a beast you scare off. It's a siege that learned to walk."),
                    Choice("I haven't seen it yet.",
                        "Don't sound disappointed. You've been spared a lesson most learn too late.",
                        "When you do see it, remember: distance is safety, cover is survival, noise is invitation.")
                }),
                Npc("Since you've returned, I'll tell you what I tell almost no one."),
                Npc("There used to be a bell tower beyond the broken arch. From there you can see the whole patrol loop."),
                Npc("I can't climb it anymore. My knees argue with every stone step. But a younger pair of legs might manage."),
                Choice(new[]
                {
                    Choice("I'll find the bell tower.",
                        "If you do, watch before you act.",
                        "Count how long it pauses at each marker. Count how long it takes to return.",
                        "Knowledge is the only weapon here that doesn't dull."),
                    Choice("Is there another route around the patrol?",
                        "There's a narrow cut behind the collapsed chapel.",
                        "It smells like wet stone and old iron, but it keeps you out of the open.",
                        "Mind your footing. One fall and the noise will do the rest."),
                    Choice("What happened to this place?",
                        "A trade town. A bad season. A worse decision by people who thought walls were enough.",
                        "When the thing arrived, the gates held for three days. Then they didn't.",
                        "I survived because I was copying maps in a cellar. Shame keeps you alive as often as wisdom.")
                }),
                Npc("One more thing. If you speak with me again after you've walked the patrol loop yourself, I'll share what the old maps didn't show."),
                Npc("For now, keep your head down and your path broken. The ruins are watching, even when the creature isn't.")
            }
        };
    }

    static DialogueStage BuildTrustedAlly()
    {
        return new DialogueStage
        {
            stageName = "Trusted Ally",
            stageDescription = "Elder Mara trusts the player and reveals deeper lore about the ruins.",
            nodes = new[]
            {
                Npc("There you are. I knew you'd live if you kept listening."),
                Npc("Sit a moment if you can spare it. At my age, conversations feel like rare weather."),
                Npc("You've walked the loop now. I can see it in how you stop before open ground."),
                Choice(new[]
                {
                    Choice("I'm ready to learn what the maps hid.",
                        "Good. Then listen as if your life still depends on it, because it does.",
                        "The patrol isn't random. It follows a path burned into the stone by old ward-markers.",
                        "Break enough markers and the loop weakens. I don't know what happens then. Nothing good, likely."),
                    Choice("I came to thank you.",
                        "Mara exhales, relief softening her face.",
                        "Then thank me by leaving this place alive.",
                        "Gratitude is sweet. Survival is sweeter."),
                    Choice("Will you leave with me?",
                        "Leave? No. These stones are my ledger now.",
                        "But I'll give you what I can: names, routes, and warnings enough to fill a book.")
                }),
                Npc("Long ago, the traders here paid for protection with coin. When coin ran out, they paid with people."),
                Npc("The thing outside isn't a punishment from the sky. It's what was bought and never un-bought."),
                Npc("That doesn't make it fair. It makes it old. Old dangers are the hardest to kill."),
                Choice(new[]
                {
                    Choice("Is there a way to stop it permanently?",
                        "Permanently? I wouldn't promise that to a saint.",
                        "But there are stories of a sealed gate beneath the chapel cut.",
                        "If such a gate exists, it would need a key, a sacrifice, or a fool. Often all three."),
                    Choice("What should I do next?",
                        "Map what you've seen. Mark where sight breaks. Mark where sound carries.",
                        "Then decide whether you're here to escape, to explore, or to fix what broke.",
                        "Each of those paths kills different kinds of people."),
                    Choice("Tell me about the people who lived here.",
                        "There was a baker who whistled. A boy who collected bell fragments.",
                        "A guard captain who locked the wrong gate because panic makes fools of the brave.",
                        "I remember them because someone must. Memory is the last kindness we can offer.")
                }),
                Npc("You've earned more than warnings now. You've earned honesty."),
                Npc("If the patrol ever stops returning home, don't celebrate. Something worse may have woken."),
                Npc("Come back when you can. I'll be here with names, maps, and whatever wisdom age hasn't stolen yet."),
                Npc("Until then, traveler: break line of sight, break pride, and break only when you must.")
            }
        };
    }

    static DialogueNode Npc(string text)
    {
        return new DialogueNode
        {
            nodeType = DialogueNodeType.NpcLine,
            text = text
        };
    }

    static DialogueNode Choice(DialogueChoice[] choices)
    {
        return new DialogueNode
        {
            nodeType = DialogueNodeType.PlayerChoice,
            choices = choices
        };
    }

    static DialogueChoice Choice(string playerText, params string[] npcResponses)
    {
        return new DialogueChoice
        {
            playerText = playerText,
            npcResponses = npcResponses
        };
    }
}
#endif
