package com.corsairlabs.elysian.games;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import com.google.android.gms.games.GamesSignInClient;
import com.google.android.gms.games.PlayGames;
import com.google.android.gms.tasks.Task;

public final class PlayGamesBridge {
    private static final String TAG = "ElysianPlayGames";
    private static volatile boolean authenticated;
    private static volatile boolean opening;
    private static volatile String message = "";

    private PlayGamesBridge() { }

    public static boolean isConfigured(Context context) {
        return context.getString(R.string.elysian_play_games_id).matches("[0-9]{4,}");
    }

    public static boolean isAuthenticated() {
        return authenticated;
    }

    public static String getMessage() {
        return message;
    }

    public static void refresh(Activity activity) {
        if (!isConfigured(activity)) return;
        activity.runOnUiThread(() -> {
            if (opening) return;
            PlayGames.getGamesSignInClient(activity).isAuthenticated().addOnCompleteListener(result -> {
                authenticated = result.isSuccessful() && result.getResult().isAuthenticated();
            });
        });
    }

    public static void show(Activity activity, String courseId, boolean achievements) {
        if (!isConfigured(activity)) return;
        activity.runOnUiThread(() -> {
            if (opening) return;
            final String board = achievements ? "" : resource(activity, "elysian_board_", courseId);
            if (!achievements && board.isEmpty()) {
                message = "This course's leaderboard is unavailable.";
                return;
            }
            opening = true;
            message = "Opening Google Play Games…";
            final GamesSignInClient signIn = PlayGames.getGamesSignInClient(activity);
            signIn.isAuthenticated().addOnCompleteListener(check -> {
                authenticated = check.isSuccessful() && check.getResult().isAuthenticated();
                if (authenticated) {
                    openPage(activity, board, achievements);
                    return;
                }
                signIn.signIn().addOnCompleteListener(result -> {
                    authenticated = result.isSuccessful() && result.getResult().isAuthenticated();
                    if (authenticated) {
                        openPage(activity, board, achievements);
                    } else {
                        opening = false;
                        message = "Play Games sign-in is unavailable. Your score is saved on this device.";
                    }
                });
            });
        });
    }

    private static void openPage(Activity activity, String board, boolean achievements) {
        final Task<Intent> request = achievements ? PlayGames.getAchievementsClient(activity).getAchievementsIntent()
                : PlayGames.getLeaderboardsClient(activity).getLeaderboardIntent(board);
        request.addOnCompleteListener(result -> {
            opening = false;
            if (result.isSuccessful() && !activity.isFinishing()) {
                final Intent intent = result.getResult();
                activity.startActivityForResult(intent, achievements ? 4102 : 4101);
                message = "";
            } else {
                message = "Google Play Games couldn't open. Try again when you're online.";
                Log.w(TAG, "Play Games page unavailable", result.getException());
            }
        });
    }

    public static void submit(Activity activity, String courseId, long score) {
        if (!authenticated || score <= 0) return;
        final String board = resource(activity, "elysian_board_", courseId);
        if (board.isEmpty()) return;
        activity.runOnUiThread(() -> PlayGames.getLeaderboardsClient(activity).submitScore(board, score));
    }

    public static void unlock(Activity activity, String key) {
        if (!authenticated) return;
        final String id = resource(activity, "elysian_achievement_", key);
        if (id.isEmpty()) return;
        activity.runOnUiThread(() -> PlayGames.getAchievementsClient(activity).unlock(id));
    }

    private static String resource(Context context, String prefix, String key) {
        final int id = context.getResources().getIdentifier(prefix + key.replace('-', '_'), "string", context.getPackageName());
        return id == 0 ? "" : context.getString(id);
    }
}
