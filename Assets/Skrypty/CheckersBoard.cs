﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckersBoard : MonoBehaviour
{
    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

    private Piece selectedPiece;
    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag; 
    private void Start()
    {
        GenerateBoard();
    }

    private void Update()
    {
        UpdateMouseOver();

//        Debug.Log(mouseOver);     //info o koordynatach kursora

        //jeśli to jest mój ruch
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if(Input.GetMouseButtonDown(0))
                SelectPiece(x, y);

            if(Input.GetMouseButtonUp(0))
            {
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
            }
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

    private void SelectPiece(int x, int y)
    {
        //poza zakresem
        if(x < 0 || x >= pieces.Length || y < 0 || y >= pieces.Length)
            return;

        Piece p = pieces[x, y];
        if(p != null)
        {
            selectedPiece = p;
            startDrag = mouseOver;
            Debug.Log(selectedPiece.name);
        }
    }

    private void TryMove(int x1, int y1, int x2, int y2)
    {
        //multiplayer wsparcie
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        selectedPiece = pieces[x1, y1];

        //sprawdzenie czy jesteśmy poza zakresem

        //jeśli zaznaczony pionek
        MovePiece(selectedPiece, x2, y2);
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
}