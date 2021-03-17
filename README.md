# Merge Request Helper
Desktop tool for work with GitLab merge requests

## Brief
It is a desktop tool that manages **git** and **GitLab** to support merge request review not from GitLab Web UI.

## Features
* Full MR cycle support: create/edit/accept/close
* Ability to see all MR that are currently in work
* Search among old MR
* Show list of recently reviewed/developed MR
* Beyond Compare 3 support: launching a diff tool and reporting new discussions for selected lines of code with hotkey
* Navigation between related discussion threads when reporting a new discussion from diff tool
* Auto-updates and notifications
* Full list of discussions with search and filters
* Custom protocol (to open links "mrhelper://gitlab-server/group/project/merge_requests/237")
* Time tracking, including calculation of total time tracked by current user
* Customizable color schemes
* Links to Jira tasks (extensible for other services)
* Filters by labels
* Custom actions (extensible)
* Color display of context diff

## GitLab API
This application uses [GitLabSharp](https://github.com/denis-adamchuk/GitLabSharp) library to work with GitLab API.

## Requirements
Visual Studio 2019 with .NET Framework 4.7.2
