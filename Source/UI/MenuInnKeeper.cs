using System.Collections;
using System.Linq;
using HarmonyLib;
using Steamworks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LCVR.UI;

public class MenuInnKeeper : MonoBehaviour
{
    private static readonly int Talking = Animator.StringToHash("Talking");
    private static readonly int Visible = Animator.StringToHash("Visible");
    private static readonly int Scare = Animator.StringToHash("Scare");
    private static readonly int Shake = Animator.StringToHash("Shake");
    
    [SerializeField] private AudioSource dialogAudio;
    [SerializeField] private AudioClip dialogVoice;
    [SerializeField] private AudioClip yellVoice;

    [SerializeField] private Animator innKeeperAnimator;

    [SerializeField] private Animator frameAnimator;
    [SerializeField] private Animator textAnimator;
    [SerializeField] private TextMeshProUGUI dialogText;

    [SerializeField] private Transform lookTarget;

    private bool countingTextTimer;
    private float textAppearTimer;

    private Coroutine dialogCoroutine;

    private Camera camera;
    private float losTime;
    private bool isTriggered;

    private void Awake()
    {
        camera = GameObject.Find("UICamera").GetComponent<Camera>();
        
        GetComponentsInChildren<Light>().Do(light => light.hideFlags = HideFlags.HideAndDontSave);
    }

    private void Update()
    {
        UpdateCountingText();
        UpdateLookTarget();
        
        if (!camera.isActiveAndEnabled)
        {
            if (!isTriggered)
                return;
            
            ResetDialog();

            return;
        }

        var los = HasLineOfSight();
        
        if (los && !isTriggered)
        {
            losTime += Time.deltaTime;

            if (losTime > 0.5f)
                OnLookedAt();
        } else if (!los && isTriggered)
        {
            ResetDialog();
        }
    }

    private void UpdateCountingText()
    {
        if (!countingTextTimer)
            return;

        textAppearTimer -= Time.unscaledDeltaTime;
        if (textAppearTimer <= 0)
            countingTextTimer = false;
    }

    private void UpdateLookTarget()
    {
        lookTarget.transform.position = camera.transform.position;
    }

    private void OnLookedAt()
    {
        isTriggered = true;

        ReadDialog(DialogGenerator.GetRandomDialog());
    }

    private IEnumerator ReadText(string text, float textSpeed)
    {
        innKeeperAnimator.SetBool(Talking, true);
        dialogText.text = text;
        dialogText.maxVisibleCharacters = 0;
        
        var characters = 0;

        while (characters < text.Length)
        {
            if (countingTextTimer)
            {
                yield return null;
                continue;
            }
            
            while (textAppearTimer <= 0)
            {
                characters += 1;
                dialogText.maxVisibleCharacters += 1;
                textAppearTimer += textSpeed;
            }

            countingTextTimer = true;

            if (characters >= text.Length)
            {
                dialogAudio.pitch = Random.Range(0.6f, 0.85f);
                dialogAudio.PlayOneShot(dialogVoice, 0.8f);
            } else if (!char.IsWhiteSpace(text[characters]) && text[characters].ToString() != ".")
            {
                dialogAudio.pitch = Random.Range(0.85f, 1.2f);
                dialogAudio.PlayOneShot(dialogVoice, 0.8f);
            }
        }

        dialogText.maxVisibleCharacters = 4000;
        innKeeperAnimator.SetBool(Talking, false);
    }

    private IEnumerator ReadDialogRoutine(Dialog dialog)
    {
        frameAnimator.SetBool(Visible, true);
        yield return new WaitForSeconds(0.3f);

        foreach (var segment in dialog.segments)
        {
            if (segment.scare)
            {
                innKeeperAnimator.SetBool(Scare, true);
                textAnimator.SetTrigger(Shake);

                dialogText.text = segment.text;
                dialogAudio.pitch = Random.Range(0.85f, 1.2f);
                dialogAudio.PlayOneShot(yellVoice, 0.8f);
            }
            else
                yield return ReadText(segment.text, dialog.textSpeed);
            
            yield return new WaitForSeconds(segment.time);
            
            innKeeperAnimator.SetBool(Scare, false);
        }

        dialogText.text = "";
        frameAnimator.SetBool(Visible, false);
    }

    public void ReadDialog(Dialog dialog)
    {
        dialogCoroutine = StartCoroutine(ReadDialogRoutine(dialog));
    }
    
    private void ResetDialog()
    {
        if (dialogCoroutine != null)
        {
            StopCoroutine(dialogCoroutine);
            dialogCoroutine = null;
        }

        frameAnimator.SetBool(Visible, false);
        innKeeperAnimator.SetBool(Scare, false);
        innKeeperAnimator.SetBool(Talking, false);

        dialogText.text = "";

        isTriggered = false;
        losTime = 0;
    }

    private bool HasLineOfSight()
    {
        return Vector3.Angle(camera.transform.forward, transform.position - camera.transform.position) < 40f;
    }
}

public struct Dialog
{
    public DialogSegment[] segments;
    public float textSpeed;
}

public struct DialogSegment
{
    public string text;
    public float time;
    public bool scare;
}

public static class DialogGenerator
{
    private static DialogSegment[][] dialogs =
    [
        [
            new DialogSegment
            {
                text = "BOO!",
                time = 1.5f,
                scare = true
            },
            new DialogSegment
            {
                text = "Did I scare you?",
                time = 1.5f
            }
        ],
        [
            new DialogSegment
            {
                text = "Did you know this company makes cars?",
                time = 2
            },
            new DialogSegment
            {
                text = "They're great for learning how to drive.",
                time = 1.5f
            },
            new DialogSegment
            {
                text = "I mean, as long as they don't explode, that is.",
                time = 1.5f
            },
            new DialogSegment
            {
                text = "I'm sure you'll be fine!",
                time = 2
            }
        ],
        [
            new DialogSegment
            {
                text = "Sometimes when I'm scared I just make sure I keep a shovel handy.",
                time = 3
            },
            new DialogSegment
            {
                text = "I've squashed quite a lot of bugs with my trusty spade.",
                time = 2.5f,
            },
            new DialogSegment
            {
                text = "Just don't smack your coworkers with it, the boss doesn't take too kindly to such \"pranks\".",
                time = 3
            }
        ],
        [
            new DialogSegment
            {
                text = "You know, I was thinking I should finally as",
                time = 0,
            },
            new DialogSegment
            {
                text = "ACK!",
                time = 2,
                scare = true,
            },
            new DialogSegment
            {
                text = "My apologies [SteamUsername], got some food lodged up my throat there.",
                time = 2
            }
        ],
        [
            new DialogSegment
            {
                text = "I once saw an employee find a gold bar, and keep it to himself.",
                time = 2
            },
            new DialogSegment
            {
                text = "Lucky fella must have gotten so rich from it he quit on the spot.",
                time = 2
            },
            new DialogSegment
            {
                text = "Never heard from him anymore though, I wonder what happened.",
                time = 2
            }
        ],
        [
            new DialogSegment
            {
                text = "I wonder if you can feed TZP to some of the fauna.",
                time = 2
            },
            new DialogSegment
            {
                text = "Wouldn't it be awesome seeing those eyeless dogs stumble around all over the place?",
                time = 3
            },
            new DialogSegment
            {
                text = "I'm sure it will definitely not make them a lot more dangerous!",
                time = 2.5f
            }
        ],
        [
            new DialogSegment
            {
                text = "You know, I really like my job",
                time = 1f
            },
            new DialogSegment
            {
                text = "I much prefer to sit at a desk and manage a hotel than whatever it is you do",
                time = 2.5f
            },
            new DialogSegment
            {
                text = "If you ever feel like it, you should book a night or two some time!",
                time = 2f
            },
            new DialogSegment
            {
                text = "I can get you 50% off, for free!",
                time = 2.5f
            }
        ],
        [
            new DialogSegment
            {
                text = "Have you noticed that the provided lunches taste a bit metallic?",
                time = 2.5f
            },
            new DialogSegment
            {
                text = "I think they're using some the scrap you sell to \"enhance\" the flavor.",
                time = 2.5f
            },
            new DialogSegment
            {
                text =
                    "They probably think they're doing you a service by \"enriching\" the food with iron, a lot of iron.",
                time = 3
            }
        ],
        [
            new DialogSegment
            {
                text = "You ever think the boss is just some weird AI computer?",
                time = 2
            },
            new DialogSegment
            {
                text = "I mean have you ever seen or spoken to him, like ever?",
                time = 2
            }
        ],
        [
            new DialogSegment
            {
                text = "If you ever accidentally step on a landmine, don't panic!",
                time = 2
            },
            new DialogSegment
            {
                text = "You will get teleported out eventually, just trust in your fellow employees.",
                time = 2.5f
            },
            new DialogSegment
            {
                text = "You do have a teleporter right?",
                time = 1.5f
            },
            new DialogSegment
            {
                text = "RIGHT?!",
                time = 2,
                scare = true
            }
        ],
        [
            new DialogSegment
            {
                text = "Remember: if you see something strange, don't panic.",
                time = 1.5f
            },
            new DialogSegment
            {
                text = "Unless it starts running towards you. Then you're allowed to panic, just a little bit.",
                time = 2.5f
            }
        ]
    ];

    public static Dialog GetRandomDialog()
    {
        var username = SteamClient.IsValid ? SteamClient.Name : "employee";

        return new Dialog
        {
            segments = dialogs[Random.Range(0, dialogs.Length)]
                .Select(diag => diag with { text = diag.text.Replace("[SteamUsername]", username) }).ToArray(),
            textSpeed = 0.032f
        };
    }
}