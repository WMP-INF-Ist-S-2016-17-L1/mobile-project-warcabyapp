using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckersBoard : MonoBehaviour
{
    public static CheckersBoard Instance {set;get;}

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    public GameObject highlightsContainer;

    public CanvasGroup alertCanvas;
    private float lastAlert;
    private bool alertActive;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0.125f, 0.5f);

    public bool isWhite;
    private bool isWhiteTurn;
    private bool hasKilled;

    private Piece selectedPiece;
    private List<Piece> forcedPieces;

    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag; 

    private Client client;

    private void Start()
    {
        Instance = this;
        client = FindObjectOfType<Client>();

        foreach(Transform t in highlightsContainer.transform)
        {
            t.position = Vector3.down * 100;
        }

        if(client)
        {
            isWhite = client.isHost;
            Alert(client.players[0].name + " VS " + client.players[1].name);
        }
        else
        {
            Alert("Białe pionki zaczynają");
        }
        isWhiteTurn = true;
        forcedPieces = new List<Piece>();
        GenerateBoard();
    }

    private void Update()
    {
        foreach(Transform t in highlightsContainer.transform)
        {
            t.Rotate(Vector3.up * 90 * Time.deltaTime);
        }

        UpdateAlert();
        UpdateMouseOver();

        //jeśli to jest mój ruch - ruch za ruch
        if((isWhite)?isWhiteTurn:!isWhiteTurn)
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if(selectedPiece != null)
                UpdatePieceDrag(selectedPiece);

            if(Input.GetMouseButtonDown(0))
                SelectPiece(x, y);

            if(Input.GetMouseButtonUp(0))
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
        }
    }

    private void UpdateMouseOver()
    {
        //jeśli to jest mój ruch
        if(!Camera.main)
        {
            Debug.Log("Nie można znaleźć głównej kamery");
            return;
        }

        RaycastHit hit;
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Plansza")))
        {
             mouseOver.x = (int)(hit.point.x - boardOffset.x);
             mouseOver.y = (int)(hit.point.z - boardOffset.z);
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }

    private void UpdatePieceDrag(Piece p)
    {
        if(!Camera.main)
        {
            Debug.Log("Nie można znaleźć głównej kamery");
            return;
        }

        RaycastHit hit;
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Plansza")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    private void SelectPiece(int x, int y)
    {
        //poza zakresem
        if(x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece p = pieces[x, y];
        if(p != null && p.isWhite == isWhite)
        {
            if(forcedPieces.Count == 0)
            {
                selectedPiece = p;
                startDrag = mouseOver;
                Debug.Log(selectedPiece.name);
            }
            else
            {
                //poszukaj pionka pod naszym, z listy wymuszonych pionków
                if(forcedPieces.Find(fp => fp == p) == null)
                    return;
                
                selectedPiece = p;
                startDrag = mouseOver;
            }
            
        }
    }

    public void TryMove(int x1, int y1, int x2, int y2)
    {
        forcedPieces = ScanForPossibleMove();
        
        //multiplayer wsparcie
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        selectedPiece = pieces[x1, y1];

        //poza zakresem
        if(x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
        {
            if(selectedPiece != null)
                MovePiece(selectedPiece, x1, y1);

            startDrag = Vector2.zero;
            selectedPiece = null;
            HighLight();
            return;
        }    

        if(selectedPiece != null)
        {
            //jeśli się nie poruszono
            if(endDrag == startDrag)
            {
                MovePiece(selectedPiece, x1, y1);
                startDrag = Vector2.zero;
                selectedPiece = null;
                HighLight();
                return;
            }

            //jeśli to możliwy ruch
            if(selectedPiece.ValidMove(pieces, x1, y1, x2, y2))
            {
                //czy cokolwiek skujemy
                //jeśli to jest skok
                if(Mathf.Abs(x2 - x1) == 2)
                {
                    Piece p = pieces[(x1 + x2)/2, (y1 + y2)/2];
                    if(p != null)
                    {
                        pieces[(x1 + x2)/2, (y1 + y2)/2] = null;
                        Destroy(p.gameObject);
                        hasKilled = true;
                    }
                }

                //gdy mieliśmy skuć cokolwiek?
                if(forcedPieces.Count != 0 && !hasKilled)
                {
                    MovePiece(selectedPiece, x1, y1);
                    startDrag = Vector2.zero;
                    selectedPiece = null;
                    HighLight();
                    return;
                }

                pieces[x2, y2] = selectedPiece;
                pieces[x1, y1] = null;
                MovePiece(selectedPiece, x2, y2);

                EndTurn();
            }
            else
            {
                MovePiece(selectedPiece, x1, y1);
                startDrag = Vector2.zero;
                selectedPiece = null;
                HighLight();
                return;
            }
        }
    }

    private void EndTurn()
    {
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        //awans pionka
        if(selectedPiece != null)
        {
            if(selectedPiece.isWhite && !selectedPiece.isKing && y == 7)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
            else if(!selectedPiece.isWhite && !selectedPiece.isKing && y == 0)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
        }

        if(client)
        {
            string msg = "CMOV|";
            msg += startDrag.x.ToString() + "|";
            msg += startDrag.y.ToString() + "|";
            msg += endDrag.x.ToString() + "|";
            msg += endDrag.y.ToString();

        client.Send(msg);
        }

        selectedPiece = null;
        startDrag = Vector2.zero;

        if(ScanForPossibleMove(selectedPiece, x, y).Count != 0 && hasKilled)
            return;

        isWhiteTurn = !isWhiteTurn;
        hasKilled = false;
        CheckVictory();

        if(!client)
        {
            isWhite = !isWhite;
            if(isWhite)
                Alert("Białe pionki");
            else
                Alert("Czarne pionki");
        }
        else
        {
            if(isWhite)
                Alert(client.players[0].name + " ruszaj się");
            else
                Alert(client.players[1].name + " ruszaj się");
        }

        ScanForPossibleMove();
    }

    private void CheckVictory()
    {
        var ps = FindObjectsOfType<Piece>();
        bool hasWhite = false, hasBlack = false;
        for(int i = 0; i < ps.Length; i++)
        {
            if(ps[i].isWhite)
                hasWhite = true;
            else
                hasBlack = true;
        }

        if(!hasWhite)
            Victory(false);
        if(!hasBlack)
            Victory(true);
    }
    
    private void Victory(bool isWhite)
    {
        if(isWhite)
            Debug.Log("Wygrała Drużyna Biała");
        else
            Debug.Log("Wygrała Drużyna Czarna");
    }

    private List<Piece> ScanForPossibleMove(Piece p, int x, int y)
    {
        forcedPieces = new List<Piece>();

        if(pieces[x, y].IsForceToMove(pieces, x, y))
            forcedPieces.Add(pieces[x, y]);

        return forcedPieces; 
    }

    private List<Piece> ScanForPossibleMove()
    {
        forcedPieces = new List<Piece>();

        //sprawdzenie wszystkich pionków
        for(int i = 0; i < 8; i++)
            for(int j = 0; j < 8; j++)
                if(pieces[i, j] != null && pieces[i, j].isWhite == isWhiteTurn)
                    if(pieces[i, j].IsForceToMove(pieces, i, j))
                        forcedPieces.Add(pieces[i, j]);

        HighLight();
        return forcedPieces;                 
    }

    private void HighLight()
    {
        foreach(Transform t in highlightsContainer.transform)
        {
            t.position = Vector3.down * 100;
        }

        if(forcedPieces.Count > 0)
            highlightsContainer.transform.GetChild(0).transform.position = forcedPieces[0].transform.position + Vector3.down * 0.1f;

        if(forcedPieces.Count > 1)
            highlightsContainer.transform.GetChild(0).transform.position = forcedPieces[0].transform.position + Vector3.down * 0.1f;
    }

    private void GenerateBoard()
    {
        //generuj białe pionki
        for(int y = 0; y < 3; y++)
        {
            bool oddRow = (y % 2 == 0);
            for(int x = 0; x < 8; x += 2)
            {
                //generuj nasze pionki
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }

        //generuj czarne pionki
        for(int y = 7; y > 4; y--)
        {
            bool oddRow = (y % 2 == 0);
            for(int x=0; x < 8; x += 2)
            {
                //generuj nasze pionki
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }
    }

    private void GeneratePiece(int x, int y)
    {
        bool isPieceWhite = (y > 3) ? false : true;
        GameObject go = Instantiate((isPieceWhite)?whitePiecePrefab:blackPiecePrefab) as GameObject;
        go.transform.SetParent(transform);
        Piece p = go.GetComponent<Piece>();
        pieces[x, y] = p;
        MovePiece(p, x, y);
    }
    
    private void MovePiece(Piece p, int x, int y)
    {
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
    }

    public void Alert(string text)
    {
        alertCanvas.GetComponentInChildren<Text>().text = text;
        alertCanvas.alpha = 1;
        lastAlert = Time.time;
        alertActive = true;
    }

    public void UpdateAlert()
    {
        if(alertActive)
        {
            if(Time.time - lastAlert > 1.5f);
            {
                alertCanvas.alpha = 1 - ((Time.time - lastAlert) - 1.5f);

                if(Time.time - lastAlert > 2.5f)
                {
                    alertActive = false;
                }
            }
        }
    }
}