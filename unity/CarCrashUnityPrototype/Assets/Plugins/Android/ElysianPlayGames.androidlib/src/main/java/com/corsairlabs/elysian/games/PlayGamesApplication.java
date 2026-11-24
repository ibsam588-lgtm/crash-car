package com.corsairlabs.elysian.games;

import android.app.Application;
import com.google.android.gms.games.PlayGamesSdk;

public final class PlayGamesApplication extends Application {
    @Override
    public void onCreate() {
        super.onCreate();
        if (PlayGamesBridge.isConfigured(this)) {
            PlayGamesSdk.initialize(this);
        }
    }
}
