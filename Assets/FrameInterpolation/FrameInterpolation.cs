using System;
using System.Collections.Generic;
using UnityEngine;

public class FrameInterpolation : MonoBehaviour {
    public static FrameInterpolation instance;

    public static bool enableMotionInterpolation;

    public static bool showMotion;

    public static bool calculateFramerate;

    public Texture source;

    public RenderTexture output;

    private RenderTexture current;

    private RenderTexture previous;

    private RenderTexture frameA;

    private RenderTexture frameB;

    private RenderTexture motionA;

    private RenderTexture motionB;

    private RenderTexture old;

    private Material sum;

    private Material motion;

    private Material blur;

    private Material lerp;

    private Material showMotionMaterial;

    private Material outputMaterial;

    public double framerate;

    [Range(-2f, 0f)]
    public double displayPosition;
    public int blurIter = 1;
    public float blurStrength = 1f;

    public int resync;

    private float previousFrameTimestamp;

    private List<float> deltaTimeCollection;

    private int deltaTimeCollectionLimit = 256;

    public void Initialize(double _framerate) {
        framerate = _framerate;
        if (_framerate == -1.0) {
            calculateFramerate = true;
        }

        enableMotionInterpolation = true;
        calculateFramerate = true;
    }

    private void Awake() {
        instance = this;
        source = Texture2D.blackTexture;
        sum = new Material(Resources.Load<Shader>("FrameInterpolation/Sum"));
        motion = new Material(Resources.Load<Shader>("FrameInterpolation/MotionCalculator"));
        blur = new Material(Resources.Load<Shader>("FrameInterpolation/Blur"));
        lerp = new Material(Resources.Load<Shader>("FrameInterpolation/Lerp"));
        showMotionMaterial = new Material(Resources.Load<Shader>("FrameInterpolation/ShowMotion"));
        outputMaterial = new Material(Resources.Load<Shader>("FrameInterpolation/Interpolate"));
        NewTextures();
        deltaTimeCollection = new List<float>();
    }

    private void OnPreRender() {
        if (source.width != output.width && source.height != output.height) {
            NewTextures();
        }

        if (!enableMotionInterpolation) {
            Graphics.Blit(source, output);
            return;
        }

        if (current == null) {
            Graphics.Blit(source, frameA);
            current = frameA;
        }

        RenderTexture curMotion = (current == frameA) ? motionA : motionB;
        RenderTexture prevMotion = (current == frameA) ? motionB : motionA;

        double num = framerate / (1.0 / (double)Time.deltaTime);
        displayPosition += num;
        if (CheckForNewFrame()) {
            if (false) return;

            if (calculateFramerate) {
                CalculateFPS();
            }

            displayPosition -= 1.0;
            Graphics.Blit(previous, old);
            current = (current == frameA) ? frameB : frameA;
            previous = (current == frameA) ? frameB : frameA;
            Graphics.Blit(source, current);
            motion.SetTexture("_Next", current);
            motion.SetTexture("_Previous", previous);
            //motion.SetFloat("_Drempel", 0.005f);
            RenderTexture tmp = RenderTexture.GetTemporary(curMotion.descriptor);
            Graphics.Blit(curMotion, curMotion, motion);
            //for (int i = 0; i < ((!showMotion) ? blurIter : 0); i++) {
            for (int i = 0; i < blurIter; i++) {
                blur.SetVector("_Direction", new Vector4(blurStrength, 0f, 0f, 0f));
                Graphics.Blit(curMotion, tmp, blur);
                blur.SetVector("_Direction", new Vector4(0f, blurStrength, 0f, 0f));
                Graphics.Blit(tmp, curMotion, blur);
            }

            lerp.SetTexture("_Last", prevMotion);
            Graphics.Blit(curMotion, tmp, lerp);
            Graphics.Blit(tmp, curMotion);
            RenderTexture.ReleaseTemporary(tmp);
        }

        if (showMotion) {
            Graphics.Blit(curMotion, output, showMotionMaterial);
            return;
        }

        resync = 0;
        if (displayPosition > 0.0) {
            displayPosition -= num;
            Debug.LogWarning("Resync (Ahead)");
            resync = 1;
        }

        if (displayPosition < -2.0) {
            displayPosition = Math.Max(displayPosition + num, 0.0);
            Debug.LogWarning("Resync (Behind)");
            resync = -1;
        }

        if (displayPosition > -1.0) {
            outputMaterial.SetTexture("_Previous", previous);
            outputMaterial.SetTexture("_Next", current);
            outputMaterial.SetTexture("_Motion", curMotion);
            outputMaterial.SetFloat("_TestShift", (float)displayPosition + 1f);
        }
        else {
            outputMaterial.SetTexture("_Previous", old);
            outputMaterial.SetTexture("_Next", previous);
            outputMaterial.SetTexture("_Motion", prevMotion);
            outputMaterial.SetFloat("_TestShift", (float)displayPosition + 2f);
        }

        Graphics.Blit(null, output, outputMaterial);
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
            if (Input.GetKeyDown(KeyCode.I)) {
                showMotion = !showMotion;
            }
        }
        else if (Input.GetKeyDown(KeyCode.I)) {
            enableMotionInterpolation = !enableMotionInterpolation;
            if (enableMotionInterpolation) {
                Graphics.Blit(source, current);
                Graphics.Blit(source, previous);
            }
        }
    }

    private bool CheckForNewFrame() {
        sum.SetTexture("_A", source);
        sum.SetTexture("_B", current);
        RenderTexture temporary = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGBFloat);
        Graphics.Blit(source, temporary, sum);
        Graphics.Blit(temporary, output);
        if (false) return true;

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = temporary;
        Texture2D texture2D = new Texture2D(temporary.width, temporary.height, TextureFormat.RGBAFloat, mipChain: false);
        texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
        texture2D.Apply();
        Color[] pixels = texture2D.GetPixels();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(temporary);
        if (pixels[0].r != 0f && pixels[0].g != 0f) {
            return pixels[0].b != 0f;
        }

        return false;
    }

    private void CalculateFPS() {
        float num = Time.realtimeSinceStartup - previousFrameTimestamp;
        previousFrameTimestamp = Time.realtimeSinceStartup;
        if (!((double)num > 0.2)) {
            deltaTimeCollection.Insert(0, num);
            if (deltaTimeCollection.Count > deltaTimeCollectionLimit) {
                deltaTimeCollection.RemoveAt(deltaTimeCollectionLimit);
            }

            double num2 = 0.0;
            for (int i = 0; i < deltaTimeCollection.Count; i++) {
                num2 += (double)deltaTimeCollection[i];
            }

            framerate = 1.0 / (num2 / (double)deltaTimeCollection.Count);
        }
    }

    private void NewTextures() {
        RenderTextureFormat format = RenderTextureFormat.ARGB32;
        if (source is RenderTexture) {
            format = ((RenderTexture)source).format;
        }

        output = new RenderTexture(source.width, source.height, 0, format);
        frameA = new RenderTexture(source.width, source.height, 0, format);
        frameB = new RenderTexture(source.width, source.height, 0, format);
        old = new RenderTexture(source.width, source.height, 0, format);
        motionA = new RenderTexture(source.width / 2, source.height / 2, 0, RenderTextureFormat.RGFloat);
        motionB = new RenderTexture(source.width / 2, source.height / 2, 0, RenderTextureFormat.RGFloat);
        Graphics.Blit(source, frameA);
        Graphics.Blit(source, frameB);
        Graphics.Blit(source, old);
    }
}