# Merge Request Helper
Desktop tool for work with GitLab merge requests

## Brief
It is a desktop tool that manages **git** and **GitLab** to support merge request review not from GitLab Web UI.

## Features
### Working with merge requests
* Full MR cycle support: create/edit/accept/close
* Ability to see all MR that are currently in work
* Search among old MR
* Show list of recently reviewed/developed MR
### Review
* Beyond Compare 3/4 support: launching a diff tool and reporting new discussions for selected lines of code with hotkey
* Color display of context diff
* Navigation between related discussion threads when reporting a new discussion from diff tool
* Full list of discussions with search and filters
* Time tracking, including calculation of total time tracked by current user
### Customization
* Custom protocol (to open links "mrhelper://gitlab-server/group/project/-/merge_requests/237")
* Custom actions (for example to send specific comments) (extensible)
* Integration with Git Extensions and Source Tree
### Other
* Auto-updates and notifications
* Customizable color schemes
* Links to Jira tasks (extensible for other services)

## GitLab API
This application uses [GitLabSharp](https://github.com/denis-adamchuk/GitLabSharp) library to work with GitLab API.

## Requirements
Visual Studio 2022 with .NET Framework 4.7.2
