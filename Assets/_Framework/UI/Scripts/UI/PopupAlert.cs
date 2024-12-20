using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Ironcow;
using System;

/// <summary>
/// �Ű����� ���� : ����, �޼�������, OKbtn����, Cancelbtn����, OKAction, CancleAction, Image, 1 or 2(�̹����ֻ��or���ϴ�)
/// </summary>
public class PopupAlert : UIBase
{
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text descText;

    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private GameObject goCancel;

    [SerializeField]
    private RectTransform rtConfirm;

    [SerializeField]
    private TMP_Text okButtonText;

    [SerializeField]
    private TMP_Text cancelButtonText;

    [SerializeField]
    private Image image;

    [SerializeField]
    private RectTransform overlay;

    [SerializeField]
    private RectTransform sizePanel;

    private string text { set => descText.text = value; }
    private string title { set => titleText.text = value; }
    private string oktext { set => okButtonText.text = value; }
    private string canceltext { set => cancelButtonText.text = value; }

    private UnityAction okCallback;
    private UnityAction<string> okInputCallback;
    private UnityAction cancelCallback;

    // �Ű����� ���� : ����, �޼�������, OKbtn, Cancelbtn, OKAction, CancleAction, Image, 1 or 2(�̹����ֻ��or���ϴ�)
    public override void Opened(object[] param)
    {
        // �̹� �����ִ°� �Ȱ��� alert�� ��� ��ŵ.
        if (UIManager.IsOpened<PopupAlert>())
        {
            if (descText.text == (string)param[0] && titleText.text == (string)param[1])
            {
                print("�̹� �����ִ� alertâ");
                return;
            }
        }

        float btnHeight = goCancel.transform.parent.GetComponent<RectTransform>().sizeDelta.y;
        float height = 0; //rectTransform.sizeDelta.y; // descText.rectTransform.sizeDelta.y

        var title = (string)param[1];
        var desc = (string)param[0];
        var oktext = (string)param[2];
        var canceltext = (string)param[3];
        try
        {
            okCallback = (UnityAction)param[4];
        }
        catch (Exception ex)
        {
            okInputCallback = (UnityAction<string>)param[4];
            inputField.text = "";
            inputField.gameObject.SetActive(true);
        }
        cancelCallback = (UnityAction)param[5];

        this.oktext = string.IsNullOrEmpty(oktext) ? LocaleDataSO.GetString("popupButtonOk") : oktext;
        this.canceltext = string.IsNullOrEmpty(canceltext) ? LocaleDataSO.GetString("popupButtonCancel") : canceltext;

        overlay.sizeDelta = new Vector2(Screen.width, Screen.height);

        text = desc.Replace("/n", "\n");
        this.title = string.IsNullOrEmpty(title) ? LocaleDataSO.GetString("popupTitle0") : title.Replace("/n", "\n");

        #region �˾�â ũ�� ����
        height += titleText.transform.parent.GetComponent<RectTransform>().sizeDelta.y;

        goCancel.SetActive(cancelCallback != null);
        if (param.Length > 6 && param[6] != null)
        {
            image.gameObject.SetActive(true);
            Texture2D tex = null;
            Sprite img = null;
            try
            {
                tex = (Texture2D)param[6];
            }
            catch(Exception e)
            {
                img = (Sprite)param[6];
            }
            if (tex != null)
            {
                img = tex.ToSprite();
            }
            image.sprite = img;
            var origin = image.rectTransform.sizeDelta;
            image.SetNativeSize();

            // �⺻ ������ �̹����� Ŭ ��
            if (image.rectTransform.sizeDelta.x >= origin.x || image.rectTransform.sizeDelta.y >= origin.y)
            {
                // ���� ���̴� ������Ű�� ������ŭ ���� ���̸� ���
                var size = image.rectTransform.sizeDelta.y / image.rectTransform.sizeDelta.x;
                image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, origin.x);
                image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, origin.x * size);
            }
            height += Mathf.Max(100, image.rectTransform.sizeDelta.y);
        }
        else
        {
            image.gameObject.SetActive(false);
        }

        if (height > 1300)
        {
            print("ũ���ִ�ġ");
            height = 1300;
        }

        //rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, sizePanel.sizeDelta.y);
        #endregion

        #region UI��ġ ����
        float uiDown = 0;
        if (title != "" && title != null)
        {
            uiDown += Mathf.Max(100, titleText.preferredHeight / 2);
            //titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, titleText.rectTransform.anchoredPosition.y - uiDown);
            uiDown += titleText.preferredHeight + btnHeight / 2;
        }

        if (param.Length > 6 && param[6] != null)
        {
            if (desc == "" || desc == null)
            {
                if (title != "" && title != null)
                {
                    uiDown += (150 - titleText.preferredHeight / 2);
                }
            }
            image.rectTransform.anchoredPosition = new Vector2(image.rectTransform.anchoredPosition.x, -uiDown);
            uiDown += image.rectTransform.sizeDelta.y;
            uiDown += btnHeight / 3;
        }

        if (desc != "" && desc != null)
        {
            uiDown += Mathf.Min(50, descText.preferredHeight / 2);
            //descText.rectTransform.anchoredPosition = new Vector2(descText.rectTransform.anchoredPosition.x, -uiDown);
        }
        #endregion

        #region UI���� ����
        //if (param.Length > 7)
        //{
        //    if ((int)param[7] == 1)
        //    {
        //        var temp = titleText.rectTransform.anchoredPosition;
        //        titleText.rectTransform.anchoredPosition = image.rectTransform.anchoredPosition;
        //        image.rectTransform.anchoredPosition = temp;
        //    }
        //    else if ((int)param[7] == 2)
        //    {
        //        var temp = descText.rectTransform.anchoredPosition;
        //        descText.rectTransform.anchoredPosition = image.rectTransform.anchoredPosition;
        //        image.rectTransform.anchoredPosition = temp;
        //    }
        //}
        #endregion

    }

    public void OnClickOk()
    {
        if (okCallback != null)
        {
            okCallback.Invoke();
        }
        else
        {
            okInputCallback?.Invoke(inputField.text);
        }
        UIManager.HideAlert();
    }

    public void OnClickCancel()
    {
        if (cancelCallback != null)
        {
            cancelCallback.Invoke();
        }
        UIManager.HideAlert();
    }
}