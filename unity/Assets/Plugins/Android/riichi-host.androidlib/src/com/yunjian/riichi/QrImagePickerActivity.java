package com.yunjian.riichi;

import android.app.Activity;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.Uri;
import android.os.Bundle;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

/** Android system photo picker. Reads only the selected URI, never edits it. */
public final class QrImagePickerActivity extends Activity {
    private static final int PICK = 7401;
    private boolean replied;
    public static void open(Activity owner, String receiver) {
        owner.runOnUiThread(() -> owner.startActivity(new Intent(owner, QrImagePickerActivity.class).putExtra("receiver", receiver)));
    }
    @Override public void onCreate(Bundle saved) {
        super.onCreate(saved);
        if (saved != null) return;
        try {
            Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            pick.setType("image/*"); pick.addCategory(Intent.CATEGORY_OPENABLE);
            pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            startActivityForResult(pick, PICK);
        } catch (Exception error) { reply("error:picker"); finish(); }
    }
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != PICK) return;
        if (result != RESULT_OK || data == null || data.getData() == null) { reply(""); finish(); return; }
        Bitmap bitmap = null;
        try {
            Uri uri = data.getData(); BitmapFactory.Options bounds = new BitmapFactory.Options(); bounds.inJustDecodeBounds = true;
            try (InputStream input = getContentResolver().openInputStream(uri)) { BitmapFactory.decodeStream(input, null, bounds); }
            if (bounds.outWidth <= 0 || bounds.outHeight <= 0) throw new IllegalArgumentException();
            BitmapFactory.Options options = new BitmapFactory.Options(); options.inSampleSize = 1;
            while (Math.max(bounds.outWidth, bounds.outHeight) / options.inSampleSize > 1600) options.inSampleSize *= 2;
            try (InputStream input = getContentResolver().openInputStream(uri)) { bitmap = BitmapFactory.decodeStream(input, null, options); }
            if (bitmap == null) throw new IllegalArgumentException();
            File cache = File.createTempFile("yunjian-qr-", ".png", getCacheDir());
            try (FileOutputStream output = new FileOutputStream(cache)) { if (!bitmap.compress(Bitmap.CompressFormat.PNG, 100, output)) throw new IllegalStateException(); }
            reply(cache.getAbsolutePath());
        } catch (Exception error) { reply("error:image"); }
        finally { if (bitmap != null) bitmap.recycle(); finish(); }
    }
    private void reply(String value) {
        if (replied) return; replied = true;
        try {
            Class.forName("com.unity3d.player.UnityPlayer").getMethod("UnitySendMessage", String.class, String.class, String.class)
                .invoke(null, getIntent().getStringExtra("receiver"), "OnImagePicked", value);
        } catch (Exception ignored) { }
    }
}
