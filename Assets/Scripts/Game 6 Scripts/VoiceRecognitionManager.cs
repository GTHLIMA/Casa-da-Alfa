
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VoiceRecognitionManager : MonoBehaviour, ISpeechToTextListener
{
public int maxAttemptsBeforeReset = 4;
private int attemptCount = 0;
private string expectedWord;
private Action<bool> callbackWhenDone;


public float listeningTimeout = 5f;


public void StartListening(string expected, Action<bool> callback)
{
expectedWord = expected;
callbackWhenDone = callback;
attemptCount = 0;
StartCoroutine(ListenCycle());
}


IEnumerator ListenCycle()
{
while (attemptCount < maxAttemptsBeforeReset)
{
// Start STT
SpeechToText.Start(this, true, false);
float elapsed = 0f;
bool gotResult = false;


while (elapsed < listeningTimeout && !gotResult)
{
elapsed += Time.deltaTime;
yield return null;
}


// Wait for OnResultReceived to set checks (this example uses a simple blocking style)
// The STT plugin you already have calls OnResultReceived when result arrives


// If plugin returns via OnResultReceived, it will compare and call the callback
// If no result, increment attempts and give hint via MainGameManager's syllable audio/hints
attemptCount++;
// play hint via MainGameManager if available
if (attemptCount == 1)
{
// first hint
}


yield return new WaitForSeconds(0.3f);
}


// if reached here, failed many times -> reset
callbackWhenDone?.Invoke(false);
}


public void OnResultReceived(string recognizedText, int? errorCode)
{
bool correct = CheckMatch(expectedWord, recognizedText);
if (correct) callbackWhenDone?.Invoke(true);
else
{
attemptCount++;
if (attemptCount >= maxAttemptsBeforeReset) callbackWhenDone?.Invoke(false);
else
{
// request another attempt
}
}
}


public void OnReadyForSpeech() { }
public void OnBeginningOfSpeech() { }
public void OnVoiceLevelChanged(float level) { }
public void OnPartialResultReceived(string partialText) { }


private bool CheckMatch(string expected, string received)
{
if (string.IsNullOrEmpty(received)) return false;
// simple compare - you can copy Levenshtein from ImageVoiceMatcher
return expected.Trim().ToLower() == received.Trim().ToLower();
}
}