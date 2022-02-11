using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
	private CharacterController2D m_character;
	private SpriteRenderer m_spriteRenderer;

	private bool m_jumpButtonPressed = false;

	public float m_speed = 5.5f;
	public float m_acceleration = 30.0f;

	public float m_jumpHeight;

	void Start()
    {
		m_character = GetComponent<CharacterController2D>();
		m_character.OnVerticalCollision2D += this.OnCollision2D;

		m_spriteRenderer = GetComponent<SpriteRenderer>();
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

	void OnCollision2D(int sign, RaycastHit2D hit)
	{
		if (sign == -1)
		{
			m_spriteRenderer.color = Color.red;
		}
		else
		{
			m_spriteRenderer.color = Color.green;
		}
	}
}
