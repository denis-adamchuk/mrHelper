import subprocess
import os

def e(str):
    str = str.replace('[', '%5B')
    str = str.replace(']', '%5D')
    str = str.replace('/', '%2F')
    return str

merge_request_project="adenis/test_project"
merge_request_iid=str(1)
merge_request_url=f"https://git.moscow.cqg/api/v4/projects/{e(merge_request_project)}/merge_requests/{e(merge_request_iid)}"

# TEST NEW MERGE REQUEST DISCUSSION
old_line=120
new_line=424

v1="54d6647c6558108d8b91d9f07f0f1237e747cfa3"
base="9c5370ce04229c97edf46051608578292430d15a"

head=v1
start=base
text="_head_v1__start_base"

include_old_line=False
include_new_line=True
body="?body=api__old_line_" + str(old_line) + ("" if include_old_line else "_not_incl__") + "new_line_" + str(new_line) + ("" if include_new_line else "_not_incl_") + text
position_old_line="&position[old_line]=" + str(old_line)
position_new_line="&position[new_line]=" + str(new_line)
position_old_path="&position[old_path]=src/op_seq.cpp"
position_new_path="&position[new_path]=src/op_seq.cpp"
position_type="&position[position_type]=text"

# Base commit SHA in the source branch
position_base_sha="&position[base_sha]=" + base

# SHA referencing commit in target branch
position_start_sha="&position[start_sha]=" + start

#SHA referencing HEAD of this merge request
position_head_sha="&position[head_sha]=" + head

api_command="/discussions"
api_command_arguments=e(\
    body + \
    (position_old_line if include_old_line else "") + \
    (position_new_line if include_new_line else "") + \
    position_old_path + \
    position_new_path + \
    position_base_sha + \
    position_start_sha + \
    position_head_sha + \
    position_type \
    )

full_merge_request_url=merge_request_url + api_command + api_command_arguments
print(full_merge_request_url)
process_one = subprocess.Popen(['curl', '--request', 'POST', '--header', "Private-Token: oHkt9LKFxXAbi4eJnh2t", '-k', full_merge_request_url])
