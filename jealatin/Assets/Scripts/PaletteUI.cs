using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PaletteUI : MonoBehaviour
{
    private Player player;
    private Enums.Color color;
    private Image img;
    [SerializeField] private Sprite[] colorPalette = new Sprite[7];

    // Start is called before the first frame update
    void Start()
    {
        color = Enums.Color.None;
        player = GameObject.Find("Player").GetComponent<Player>();
        img = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player.Color != color)
        {
            SwapColor(player.Color);
        }
    }

    void SwapColor(Enums.Color color)
    {
        color = player.Color;
        switch (color)
        {
            case Enums.Color.Blue:
                img.sprite = colorPalette[0];
                break;
            case Enums.Color.Yellow:
                img.sprite = colorPalette[1];
                break;
            case Enums.Color.Green:
                img.sprite = colorPalette[2];
                break;
            case Enums.Color.Red:
                img.sprite = colorPalette[3];
                break;
            case Enums.Color.Violet:
                img.sprite = colorPalette[4];
                break;
            case Enums.Color.Orange:
                img.sprite = colorPalette[5];
                break;
            case Enums.Color.Black:
                img.sprite = colorPalette[6];
                break;
            default:
                break;
        }
    }
}
