using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Tools;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage : UserControl, ICommandCallback
   {
      private static readonly int ProjectAndUserCacheCheckTimerInterval = 1000 * 1; // 1 second
      private static readonly int RedrawTimerInterval = 1000 * 30; // 0.5 minute
      private static readonly int PseudoTimerInterval = 100 * 1; // 0.1 second

      private readonly Timer _redrawTimer = new Timer
      {
         // This timer is needed to update "ago" timestamps
         Interval = RedrawTimerInterval
      };

      private bool _isActivePage;
      private bool _exiting = false;
      private readonly Dictionary<string, bool> _userIsMovingSplitter = new Dictionary<string, bool>();
      private readonly bool _integratedInGitExtensions;
      private readonly bool _integratedInSourceTree;

      private readonly IEnumerable<string> _keywords;
      private readonly ColorScheme _colorScheme;
      private readonly TrayIcon _trayIcon;
      private readonly Markdig.MarkdownPipeline _mdPipeline;
      private LocalCommitStorageFactory _storageFactory;
      private GitDataUpdater _gitDataUpdater;
      private IDiffStatisticProvider _diffStatProvider;
      private UserNotifier _userNotifier;
      private EventFilter _eventFilter;
      private ExpressionResolver _expressionResolver;
      private MergeRequestFilter _mergeRequestFilter;
      private MergeRequestFilter _mergeRequestFilterRecent;
      private readonly System.Windows.Forms.ToolTip _toolTip;
      private Forms.EditSearchQueryFormState _prevSearchQuery;

      private readonly DictionaryWrapper<MergeRequestKey, DateTime> _recentMergeRequests;
      private readonly DictionaryWrapper<MergeRequestKey, HashSet<string>> _reviewedRevisions;
      private readonly DictionaryWrapper<string, MergeRequestKey> _lastMergeRequestsByHosts;
      private readonly DictionaryWrapper<string, NewMergeRequestProperties> _newMergeRequestDialogStatesByHosts;
      private readonly List<MergeRequestKey> _mergeRequestsUpdatingByUserRequest = new List<MergeRequestKey>();
      private readonly Dictionary<MergeRequestKey, string> _latestStorageUpdateStatus = new Dictionary<MergeRequestKey, string>();

      private Shortcuts _shortcuts;
      private GitLabInstance _gitLabInstance;
      private DataCache _liveDataCache;
      private DataCache _searchDataCache;
      private DataCache _recentDataCache;
      private string HostName { get; }
      private User CurrentUser { get; set; }
      private GitLabVersion GitLabVersion { get; set; }
      private bool? _isApprovalStatusSupported;

      private enum EConnectionStateInternal
      {
         ConnectingLive,
         ConnectingRecent,
         Connected
      }
      private EConnectionStateInternal? _connectionStatus;

      private struct LostConnectionInfo
      {
         internal LostConnectionInfo(DateTime timeStamp)
         {
            TimeStamp = timeStamp;
         }

         internal DateTime TimeStamp { get; }
      }
      private LostConnectionInfo? _lostConnectionInfo;
   }
}

