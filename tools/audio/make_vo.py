"""Robot announcer lines. Usage: python3 -I make_vo.py OUT_DIR
Speaks each line with the macOS `say` voice, then robotises it (ring modulation + bit crush + soft clip), 22.05 kHz mono WAV.
Each key has several variants so the announcer does not repeat itself. Text lives here, audio lives in
Assets/Resources/RoadBuilder/Audio/VO/<key>_<n>.wav."""
import array, math, os, struct, subprocess, sys, tempfile, wave

VOICE = "Zarvox"
LINES = {
    "start":    ["Engines online. Let's roll!", "Systems green. Draw me a road!", "Beep boop. Time to race!"],
    "boost":    ["Boost engaged!", "Nitro!", "Hold on to your bolts!"],
    "shield":   ["Shield online.", "Force field up!"],
    "magnet":   ["Magnet active.", "Coin magnet engaged."],
    "double":   ["Double coins!", "Cha ching times two!"],
    "slowmo":   ["Time slowed.", "Slow motion."],
    "ghost":    ["Ghost road. Draw for free!", "Ink is free. Go wild!"],
    "lowink":   ["Warning. Ink low.", "Ink reserves critical."],
    "noink":    ["Out of ink!", "Tank empty. Find ink!"],
    "flip":     ["Flip!", "Nice flip!", "Spin cycle!"],
    "bigair":   ["Massive air!", "Houston, we have lift off!", "Insane air time!"],
    "combo":    ["Combo!", "Coin storm!", "Combo multiplier up!"],
    "frenzy":   ["Frenzy mode! Maximum overdrive!", "Overdrive! Everything is double!"],
    "nearmiss": ["Close call!", "That was close!"],
    "meteor":   ["Incoming! Watch the sky!", "Meteor warning!"],
    "chest":    ["Treasure unlocked!", "Chest secured!"],
    "newbest":  ["New record! You are a legend!", "Personal best. Impressive.", "Record broken. Beep boop."],
    "crash":    ["Crash detected.", "Ouch. That hurt my circuits.", "Oh no!"],
    "gameover": ["Game over. Try again, human.", "Run complete. Recharging.", "System shutdown. Just kidding. Again?"],
    "welcome":  ["Welcome back, driver.", "Garage open. Ready when you are."],
    "daily":    ["Daily reward ready!", "Free stuff detected!"],
    "mission":  ["Mission complete!", "Objective achieved!"],
    "unlock":   ["New ride unlocked!", "Unlock complete!"],
}


def robotise(src_wav, dst_wav):
    with wave.open(src_wav) as w:
        sr, n = w.getframerate(), w.getnframes()
        data = array.array("h", w.readframes(n))
    out = array.array("h")
    crush = 4
    held = 0.0
    for i, s in enumerate(data):
        x = s / 32768.0
        if i % crush == 0:
            held = x
        x = held
        ring = math.sin(2 * math.pi * 55 * i / sr)  # 55 Hz ring mod gives the metallic buzz
        y = 0.7 * x + 0.5 * x * ring
        y = math.tanh(y * 2.2)
        out.append(int(max(-1, min(1, y)) * 30000))
    with wave.open(dst_wav, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(out.tobytes())


def main(out):
    os.makedirs(out, exist_ok=True)
    tmp = tempfile.mkdtemp()
    for key, lines in LINES.items():
        for i, text in enumerate(lines):
            aiff = os.path.join(tmp, "a.aiff")
            raw = os.path.join(tmp, "r.wav")
            subprocess.run(["say", "-v", VOICE, "-r", "175", "-o", aiff, text], check=True)
            subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", aiff, raw], check=True)
            robotise(raw, os.path.join(out, f"{key}_{i}.wav"))
            print("ok", key, i, flush=True)


if __name__ == "__main__":
    main(sys.argv[1])
