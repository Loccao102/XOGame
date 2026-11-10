mergeInto(LibraryManager.library, {
    XOPlayable_InitSDK: function () {
        try {
            // YouTube Playables SDK lifecycle hooks
            if (typeof window !== 'undefined' && window.ytgame) {
                if (window.ytgame.game && typeof window.ytgame.game.firstFrameReady === 'function') {
                    window.ytgame.game.firstFrameReady();
                }
                if (window.ytgame.game && typeof window.ytgame.game.gameReady === 'function') {
                    window.ytgame.game.gameReady();
                }
                if (window.ytgame.system && typeof window.ytgame.system.onAudioEnabledChange === 'function') {
                    window.ytgame.system.onAudioEnabledChange(function (isAudioEnabled) {
                        SendMessage('XOGameManager', 'OnExternalAudioStateChanged', isAudioEnabled ? 1 : 0);
                    });
                }
                if (window.ytgame.system && typeof window.ytgame.system.onPause === 'function') {
                    window.ytgame.system.onPause(function () {
                        SendMessage('XOGameManager', 'OnExternalPauseStateChanged', 1);
                    });
                }
                if (window.ytgame.system && typeof window.ytgame.system.onResume === 'function') {
                    window.ytgame.system.onResume(function () {
                        SendMessage('XOGameManager', 'OnExternalPauseStateChanged', 0);
                    });
                }
            }
        } catch (e) {
            console.warn('[XOPlayable] SDK Init warning:', e);
        }
    },

    XOPlayable_TriggerCTA: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        try {
            // 1. Google Ads App Campaign HTML5 Playable ExitApi
            if (typeof window !== 'undefined' && window.ExitApi && typeof window.ExitApi.exit === 'function') {
                window.ExitApi.exit();
                return;
            }
            // 2. MRAID 2.0 (AppLovin, Unity Ads, Vungle, etc.)
            if (typeof window !== 'undefined' && window.mraid && typeof window.mraid.open === 'function') {
                window.mraid.open(url);
                return;
            }
            // 3. ironSource DAPI
            if (typeof window !== 'undefined' && window.dapi && typeof window.dapi.openStoreUrl === 'function') {
                window.dapi.openStoreUrl();
                return;
            }
            // 4. Meta / Facebook Playable Ad
            if (typeof window !== 'undefined' && window.FbPlayableAd && typeof window.FbPlayableAd.onCTAClick === 'function') {
                window.FbPlayableAd.onCTAClick();
                return;
            }
            // 5. Fallback standard browser open
            if (typeof window !== 'undefined') {
                window.open(url, '_blank');
            }
        } catch (e) {
            if (typeof window !== 'undefined') {
                window.open(url, '_blank');
            }
        }
    },

    XOPlayable_SendScore: function (score) {
        try {
            if (typeof window !== 'undefined' && window.ytgame && window.ytgame.engagement && typeof window.ytgame.engagement.sendScore === 'function') {
                window.ytgame.engagement.sendScore({ value: score });
            }
        } catch (e) {
            console.warn('[XOPlayable] SendScore warning:', e);
        }
    },

    XOPlayable_TriggerHaptic: function (durationMs) {
        try {
            if (typeof navigator !== 'undefined' && typeof navigator.vibrate === 'function') {
                navigator.vibrate(durationMs);
            }
        } catch (e) {
            // Ignore on unsupported browsers
        }
    }
});

