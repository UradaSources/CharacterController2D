using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
	private CharacterController2D m_character;

	private bool m_jumpButtonPressed = false;

	public float m_speed = 5.5f;
	public float m_acceleration = 30.0f;

	public float m_jumpHeight;

    void Start()
    {
		m_character = GetComponent<CharacterController2D>();
    }

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			m_jumpButtonPressed = true;
		}
	}

	void FixedUpdate()
	{
		float direction = Input.GetAxisRaw("Horizontal");
		m_character.SimpleMove((int)direction, m_speed, m_acceleration);

		if (m_jumpButtonPressed)
		{
			m_character.TrySimpleJump(m_jumpHeight);
		}

		m_jumpButtonPressed = false;
	}
}
