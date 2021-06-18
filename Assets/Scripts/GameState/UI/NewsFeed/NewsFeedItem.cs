using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using UnityEngine.EventSystems;

public class NewsFeedItem : MonoBehaviour, IPointerClickHandler {

    public Button NextPage;
    public Button PreviousPage;
    public TMP_Text Title;
    public TMP_Text CreationDate;
    public TMP_Text NewsBody;
    public TMP_Text PageCount;
    void Start() {
        NextPage.onClick.AddListener(OnNextPage);
        PreviousPage.onClick.AddListener(OnPreviousPage);
        NewsBody.ForceMeshUpdate();
    }
    public void Show(string full) {
        StringReader sr = new StringReader(full);
        Title.text = sr.ReadLine();
        try {
            CreationDate.text = DateTime.Parse(sr.ReadLine(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch(Exception e) {
            Debug.Log(e.Message);
            CreationDate.gameObject.SetActive(false);
        }
        NewsBody.text = sr.ReadToEnd();
        sr.Dispose();
        StartCoroutine(CheckPages());
        PreviousPage.interactable = false;

    }

    private IEnumerator CheckPages() {
        yield return new WaitForEndOfFrame();
        if (NewsBody.textInfo.pageCount <= 1) {
            NextPage.transform.parent.gameObject.SetActive(false);
        } else {
            PageCount.text = 1 + "/" + NewsBody.textInfo.pageCount;
        }
        yield return null;
    }

    private void OnNextPage() {  
        NewsBody.pageToDisplay++;
        if(NewsBody.pageToDisplay >= NewsBody.textInfo.pageCount) {
            NextPage.interactable = false;
        }
        PreviousPage.interactable = true;
        PageCount.text = NewsBody.pageToDisplay + "/" + NewsBody.textInfo.pageCount;
    }

    private void OnPreviousPage() {
        NewsBody.pageToDisplay--;
        if (NewsBody.pageToDisplay == 1)
            PreviousPage.interactable = false;
        NextPage.interactable = true;
        PageCount.text = NewsBody.pageToDisplay + "/" + NewsBody.textInfo.pageCount;
    }
    public void OnPointerClick(PointerEventData eventData) {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(NewsBody, Input.mousePosition, null);
        if (linkIndex != -1) { // was a link clicked?
            TMP_LinkInfo linkInfo = NewsBody.textInfo.linkInfo[linkIndex];
            // open the link id as a url, which is the metadata we added in the text field
            Application.OpenURL(linkInfo.GetLinkID());
        }
    }

}
