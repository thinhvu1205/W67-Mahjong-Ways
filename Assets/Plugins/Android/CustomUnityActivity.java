import android.os.Bundle;
import android.util.Log;
import com.unity3d.player.UnityPlayerActivity;

public class CustomUnityActivity extends UnityPlayerActivity {
    @Override
    protected void onDestroy() {
        super.onDestroy();
        Log.d("UnityKill", "CustomUnityActivity: onDestroy called");
        // Bạn có thể gọi UnitySendMessage nếu cần gửi về C#
    }
}