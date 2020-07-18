import os
import json

commit_hash = os.getenv("GIT_HASH")
rev = os.getenv("GIT_REV")
file_path = "Source/CustomAvatar/manifest.json"

with open(file_path, "r") as json_file:
    obj = json.load(json_file)

semver = obj["version"]
has_metadata = semver.index("+") > 0
version = semver[0:semver.index("+")]

if (rev is not None and len(rev) > 0):
    if rev != ("v" + version):
        print("Git tag does not match manifest version")
        exit(-1)

    obj["version"] = version
else:
    if (commit_hash is None or len(commit_hash) == 0):
        print("Commit hash not found in environment")
        exit(-1)

    obj["version"] = version + "+git." + commit_hash



with open(file_path, "w") as json_file:
    json.dump(obj, json_file, indent=2)