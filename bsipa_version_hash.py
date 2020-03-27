import os
import json

commit_hash = os.getenv("GIT_HASH")
rev = os.getenv("GIT_REV")
file_path = "Source/CustomAvatar/manifest.json"

if (rev is not None and len(rev) > 0):
    print("Skipping commit hash")
    exit()

if (commit_hash is None or len(commit_hash) == 0):
    print("Commit hash not found in environment")
    exit(-1)

with open(file_path, "r") as json_file:
    obj = json.load(json_file)

version = obj["version"]

obj["version"] = version[0:version.index("-", version.index("-") + 1)] + "-" + commit_hash

with open(file_path, "w") as json_file:
    json.dump(obj, json_file, indent=2)