"""Summarize sampled thread time under NPC work; no access to the game process."""
import collections
import json
import sys

with open(sys.argv[1], encoding="utf-8-sig") as source:
    data = json.load(source)
names = [frame["name"] for frame in data["shared"]["frames"]]
inclusive = collections.Counter()
leaves = collections.Counter()
stacks = collections.Counter()
for profile in data["profiles"]:
    stack = []
    previous = profile["startValue"]
    for event in profile["events"]:
        elapsed = event["at"] - previous
        if elapsed > 0 and any("NpcService" in names[index] for index in stack):
            for index in set(stack):
                inclusive[names[index]] += elapsed
            if stack:
                leaves[names[stack[-1]]] += elapsed
                stacks[tuple(names[index] for index in stack[-9:])] += elapsed
        previous = event["at"]
        if event["type"] == "O":
            stack.append(event["frame"])
        else:
            assert stack.pop() == event["frame"]
for label, counts in [("INCLUSIVE MS", inclusive), ("LEAF MS", leaves)]:
    print(label)
    for name, elapsed in counts.most_common(25):
        print(f"{elapsed:10.1f} {name}")
print("TOP NPC STACKS")
for stack, elapsed in stacks.most_common(12):
    print(f"{elapsed:.1f} ms\n  " + "\n  ".join(stack))
