mergeInto(LibraryManager.library, {
    XOPlayable_InitSDK: function (callbackObjNamePtr) {
        var cbObj = 'XOGameManager';
        try {
            if (callbackObjNamePtr) {
                var parsed = UTF8ToString(callbackObjNamePtr);
                if (parsed) cbObj = parsed;
            }
        } catch (e) {}

        try {
            if (typeof window !== 'undefined') {
                // Attach browser visibility/blur mute hooks once for non-YouTube containers
                if (!window.__xoPlayableLifecycleBound && typeof document !== 'undefined') {
                    window.__xoPlayableLifecycleBound = true;
                    var syncHidden = function (hidden) {
                        try {
                            SendMessage(cbObj, 'OnBrowserVisibilityChanged', hidden ? 1 : 0);
                        } catch (err) {}
                    };
                    document.addEventListener('visibilitychange', function () {
                        syncHidden(!!document.hidden);
                    });
                    window.addEventListener('blur', function () { syncHidden(true); });
                    window.addEventListener('focus', function () { syncHidden(!!document.hidden); });
                    window.addEventListener('pagehide', function () { syncHidden(true); });
                }

                // YouTube Playables SDK lifecycle hooks
                if (window.ytgame) {
                    if (window.ytgame.game && typeof window.ytgame.game.firstFrameReady === 'function') {
                        window.ytgame.game.firstFrameReady();
                    }
                    if (window.ytgame.game && typeof window.ytgame.game.gameReady === 'function') {
                        window.ytgame.game.gameReady();
                    }
                    if (window.ytgame.system) {
                        if (typeof window.ytgame.system.isAudioEnabled === 'function') {
                            SendMessage(cbObj, 'OnExternalAudioStateChanged', window.ytgame.system.isAudioEnabled() ? 1 : 0);
                        }
                        if (typeof window.ytgame.system.onAudioEnabledChange === 'function') {
                            window.ytgame.system.onAudioEnabledChange(function (isAudioEnabled) {
                                SendMessage(cbObj, 'OnExternalAudioStateChanged', isAudioEnabled ? 1 : 0);
                            });
                        }
                        if (typeof window.ytgame.system.onPause === 'function') {
                            window.ytgame.system.onPause(function () {
                                SendMessage(cbObj, 'OnExternalPauseStateChanged', 1);
                            });
                        }
                        if (typeof window.ytgame.system.onResume === 'function') {
                            window.ytgame.system.onResume(function () {
                                SendMessage(cbObj, 'OnExternalPauseStateChanged', 0);
                            });
                        }
                    }
                }
            }
        } catch (e) {
            console.warn('[XOPlayable] SDK Init warning:', e);
        }
    },

    XOPlayable_TriggerCTA: function (urlPtr) {
        var url = urlPtr ? UTF8ToString(urlPtr) : '';
        try {
            // 1. Google Ads App Campaign HTML5 Playable ExitApi
            if (typeof window !== 'undefined' && window.ExitApi && typeof window.ExitApi.exit === 'function') {
                window.ExitApi.exit();
                return;
            }
            // 2. MRAID 2.0 (AppLovin, Unity Ads, Vungle, etc.)
            if (typeof window !== 'undefined' && window.mraid && typeof window.mraid.open === 'function') {
                if (url) window.mraid.open(url);
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
            // 5. Fallback standard browser open (skipped in standalone playable mode when url is empty)
            if (typeof window !== 'undefined' && url) {
                window.open(url, '_blank');
            }
        } catch (e) {
            if (typeof window !== 'undefined' && url) {
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

