// Amplify Hub
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyHub
{
	public class Window : EditorWindow
	{
		private enum Section { Home, Installed, Catalog, Settings }
		private enum Tab { Description, Wiki, Samples, Tutorials }

		// Skin-invariant
		private static readonly Color ColorNavActiveBg = new Color( 0.25f, 0.45f, 0.75f, 0.3f );
		private static readonly Color ColorNavHoverBg  = new Color( 0.25f, 0.45f, 0.75f, 0.12f );
		private static readonly Color ColorSelectedBg  = new Color( 0.25f, 0.45f, 0.75f, 0.25f );
		private static readonly Color ColorFreeTagBg   = new Color( 0.20f, 0.58f, 0.28f, 0.5f );

		// Skin-dependent (set in InitStyles)
		private Color ColorDivider;
		private Color ColorItemSeparator;
		private Color ColorIconPlaceholder;
		private Color ColorIconBorder;
		private Color ColorTabInactiveBg;

		private const string ShowAtStartupKey = "AmplifyHub_ShowAtStartup";

		public static bool ShowAtStartup
		{
			get => EditorPrefs.GetBool( ShowAtStartupKey, true );
			set => EditorPrefs.SetBool( ShowAtStartupKey, value );
		}

		private const float DividerW    = 1f;
		private const float DividerHit  = 4f;   // px either side of divider that counts as a hit
		private const float TabH        = 22f;
		private const float ItemH       = 44f;
		private const float NavItemH    = 28f;

		private Section m_section = Section.Home;
		private Tab     m_activeTab = Tab.Description;
		private Product m_selectedInstalled = null;
		private Product m_selectedCatalog   = null;

		private Product SelectedProduct
		{
			get => m_section == Section.Installed ? m_selectedInstalled : m_selectedCatalog;
			set { if ( m_section == Section.Installed ) m_selectedInstalled = value; else m_selectedCatalog = value; }
		}
		private Vector2 m_productListScroll;
		private Vector2 m_contentScroll;
		private Vector2 m_homeScroll;

		// Resizable panel widths
		private float m_leftW;
		private float m_middleW;
		private bool  m_widthsInitialized  = false;
		[SerializeField] private bool m_positionInitialized = false;

		// Divider drag state
		private bool  m_draggingLeft  = false;
		private bool  m_draggingRight = false;
		private float m_dragStartX;
		private float m_dragStartWidth;

		private GUIStyle m_navItemStyle;
		private GUIStyle m_navItemActiveStyle;
		private GUIStyle m_listItemStyle;
		private GUIStyle m_listItemDescStyle;
		private GUIStyle m_tabStyle;
		private GUIStyle m_tabActiveStyle;
		private GUIStyle m_productTitleStyle;
		private GUIStyle m_iconLabelStyle;
		private GUIStyle m_versionStyle;
		private GUIStyle m_tagLabelStyle;
		private GUIStyle m_bannerLabelStyle;
		private GUIStyle m_settingsHeaderStyle;
		private GUIStyle m_homeSectionTitleStyle;
		private GUIStyle m_homeBodyStyle;
		private GUIStyle m_homeProductTitleStyle;
		private GUIStyle m_homeProductBodyStyle;
		private bool m_stylesInitialized = false;
		private bool m_lastProSkin       = false;

		public static void ResetLayout()
		{
			foreach ( var w in Resources.FindObjectsOfTypeAll<Window>() )
			{
				w.m_positionInitialized = false;
				w.m_widthsInitialized   = false;
			}
			Open();
		}

		[MenuItem( "Window/Amplify Hub" )]
		public static void Open()
		{
			var window = GetWindow<Window>( "Amplify Hub" );
			window.minSize = new Vector2( 720f, 480f );
			if ( !window.m_positionInitialized )
			{
				window.m_positionInitialized = true;
				const float w = 1100f, h = 680f;
				var main = EditorGUIUtility.GetMainWindowPosition();
				window.position = new Rect(
					Mathf.Round( main.x + ( main.width  - w ) * 0.5f ),
					Mathf.Round( main.y + ( main.height - h ) * 0.5f ),
					w, h );
			}
		}

		private void OnEnable()
		{
			wantsMouseMove = true;
		}

		private void InitStyles()
		{
			bool proSkin = EditorGUIUtility.isProSkin;
			if ( m_stylesInitialized && m_lastProSkin == proSkin )
			{
				return;
			}
			m_lastProSkin = proSkin;

			ColorDivider         = proSkin ? new Color( 0.13f, 0.13f, 0.13f ) : new Color( 0.55f, 0.55f, 0.55f );
			ColorItemSeparator   = proSkin ? new Color( 0.30f, 0.30f, 0.30f ) : new Color( 0.68f, 0.68f, 0.68f );
			ColorIconPlaceholder = proSkin ? new Color( 0.22f, 0.22f, 0.22f ) : new Color( 0.73f, 0.73f, 0.73f );
			ColorIconBorder      = proSkin ? new Color( 0.35f, 0.35f, 0.35f ) : new Color( 0.60f, 0.60f, 0.60f );
			ColorTabInactiveBg   = proSkin ? new Color( 0.17f, 0.17f, 0.17f ) : new Color( 0.70f, 0.70f, 0.70f );

			m_navItemStyle = new GUIStyle( EditorStyles.label )
			{
				padding   = new RectOffset( 10, 8, 0, 0 ),
				alignment = TextAnchor.MiddleLeft,
				fontSize  = 12
			};

			m_navItemActiveStyle = new GUIStyle( m_navItemStyle );
			m_navItemActiveStyle.normal.textColor = EditorGUIUtility.isProSkin
				? new Color( 0.5f, 0.8f, 1f )
				: new Color( 0.1f, 0.35f, 0.7f );

			m_listItemStyle = new GUIStyle( EditorStyles.label )
			{
				alignment = TextAnchor.MiddleLeft,
				padding   = new RectOffset( 0, 0, 0, 0 )
			};

			m_listItemDescStyle = new GUIStyle( m_listItemStyle )
			{
				fontSize = EditorStyles.miniLabel.fontSize,
				normal   = { textColor = proSkin ? new Color( 0.55f, 0.55f, 0.55f ) : new Color( 0.40f, 0.40f, 0.40f ) }
			};

			m_tabStyle = new GUIStyle( EditorStyles.label )
			{
				alignment = TextAnchor.MiddleCenter,
				padding   = new RectOffset( 14, 14, 0, 0 ),
				fontSize  = 11
			};

			m_tabActiveStyle = new GUIStyle( m_tabStyle );

			m_productTitleStyle = new GUIStyle( EditorStyles.boldLabel )
			{
				fontSize  = 16,
				wordWrap  = false,
				alignment = TextAnchor.MiddleLeft
			};

			m_iconLabelStyle = new GUIStyle( EditorStyles.centeredGreyMiniLabel )
			{
				alignment = TextAnchor.MiddleCenter
			};

			m_versionStyle = new GUIStyle( EditorStyles.miniLabel )
			{
				alignment = TextAnchor.MiddleRight,
				normal    = { textColor = proSkin ? new Color( 0.55f, 0.55f, 0.55f ) : new Color( 0.40f, 0.40f, 0.40f ) }
			};

			m_tagLabelStyle = new GUIStyle( EditorStyles.miniLabel )
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold,
				fontSize  = 8,
				normal    = { textColor = proSkin
					? new Color( 1f, 1f, 1f, 0.6f )
					: new Color( 0.1f, 0.1f, 0.1f, 0.7f ) }
			};

			m_bannerLabelStyle = new GUIStyle( EditorStyles.centeredGreyMiniLabel )
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize  = 18,
				fontStyle = FontStyle.Bold
			};

			m_settingsHeaderStyle = new GUIStyle( EditorStyles.boldLabel )
			{
				fontSize = 13
			};

			m_homeSectionTitleStyle = new GUIStyle( EditorStyles.boldLabel )
			{
				fontSize  = 22,
				wordWrap  = false,
				alignment = TextAnchor.MiddleLeft
			};

			m_homeBodyStyle = new GUIStyle( EditorStyles.wordWrappedLabel )
			{
				fontSize = 13
			};

			m_homeProductTitleStyle = new GUIStyle( EditorStyles.boldLabel )
			{
				fontSize  = 22,
				wordWrap  = false,
				alignment = TextAnchor.UpperLeft
			};

			m_homeProductBodyStyle = new GUIStyle( EditorStyles.label )
			{
				fontSize  = 22,
				wordWrap  = false,
				alignment = TextAnchor.UpperLeft,
				normal    = { textColor = proSkin ? new Color( 0.7f, 0.7f, 0.7f ) : new Color( 0.3f, 0.3f, 0.3f ) }
			};

			m_stylesInitialized = true;
		}

		private void OnGUI()
		{
			InitStyles();

			if ( !m_widthsInitialized )
			{
				m_leftW             = 160f;
				m_middleW           = 290f;
				m_widthsInitialized = true;
			}

			float h          = position.height;
			float ppp        = EditorGUIUtility.pixelsPerPoint;
			float panelDivW  = 2f / ppp;
			bool  showProductList = m_section == Section.Installed || m_section == Section.Catalog;

			// Snap every panel boundary to an exact screen pixel so BeginArea clips
			// and DrawRect positions always agree, eliminating 1px seams.
			float leftDivX  = Mathf.Round( m_leftW * ppp ) / ppp;
			float midX      = Mathf.Round( ( leftDivX  + panelDivW ) * ppp ) / ppp;
			float rightDivX = Mathf.Round( ( midX      + m_middleW ) * ppp ) / ppp;
			float contentX  = Mathf.Round( ( rightDivX + panelDivW ) * ppp ) / ppp;
			float middleW   = rightDivX - midX;
			float contentW  = position.width - contentX;
			float fullW     = position.width - midX;

			// ── Divider interaction (before any layout areas) ──────────────────
			HandleDividerDrag( leftDivX, showProductList ? rightDivX : -1f, h, panelDivW );

			// ── Cursor rects ───────────────────────────────────────────────────
			EditorGUIUtility.AddCursorRect(
				new Rect( leftDivX - DividerHit, 0, panelDivW + DividerHit * 2, h ),
				MouseCursor.ResizeHorizontal );

			if ( showProductList )
			{
				EditorGUIUtility.AddCursorRect(
					new Rect( rightDivX - DividerHit, 0, panelDivW + DividerHit * 2, h ),
					MouseCursor.ResizeHorizontal );
			}

			// ── Draw dividers ──────────────────────────────────────────────────
			EditorGUI.DrawRect( new Rect( leftDivX, 0, panelDivW, h ), ColorDivider );
			if ( showProductList )
			{
				EditorGUI.DrawRect( new Rect( rightDivX, 0, panelDivW, h ), ColorDivider );
			}

			// ── Draw panels ────────────────────────────────────────────────────
			GUILayout.BeginArea( new Rect( 0, 0, m_leftW, h ), GUIStyle.none );
			DrawNavPanel();
			GUILayout.EndArea();

			if ( showProductList )
			{
				GUILayout.BeginArea( new Rect( midX, 0, middleW, h ), GUIStyle.none );
				DrawProductList();
				GUILayout.EndArea();

				GUILayout.BeginArea( new Rect( contentX, 0, contentW, h ), GUIStyle.none );
				DrawContentPanel();
				GUILayout.EndArea();
			}
			else
			{
				GUILayout.BeginArea( new Rect( midX, 0, fullW, h ), GUIStyle.none );
				if ( m_section == Section.Home )
				{
					DrawHomeBanner( fullW, h );
				}
				else
				{
					DrawSettingsPanel();
				}
				GUILayout.EndArea();
			}
		}

		// ── Divider drag ───────────────────────────────────────────────────────

		private void HandleDividerDrag( float leftDivX, float rightDivX, float h, float panelDivW )
		{
			var   e        = Event.current;
			bool  hasRight = rightDivX >= 0f;

			var leftHit  = new Rect( leftDivX  - DividerHit, 0, panelDivW + DividerHit * 2, h );
			var rightHit = hasRight
				? new Rect( rightDivX - DividerHit, 0, panelDivW + DividerHit * 2, h )
				: Rect.zero;

			if ( e.type == EventType.MouseDown && e.button == 0 )
			{
				if ( leftHit.Contains( e.mousePosition ) )
				{
					m_draggingLeft  = true;
					m_dragStartX    = e.mousePosition.x;
					m_dragStartWidth = m_leftW;
					e.Use();
				}
				else if ( hasRight && rightHit.Contains( e.mousePosition ) )
				{
					m_draggingRight  = true;
					m_dragStartX     = e.mousePosition.x;
					m_dragStartWidth = m_middleW;
					e.Use();
				}
			}
			else if ( e.type == EventType.MouseDrag && e.button == 0 )
			{
				float delta = e.mousePosition.x - m_dragStartX;

				if ( m_draggingLeft )
				{
					float maxLeft = position.width * 0.35f;
					m_leftW = Mathf.Clamp( m_dragStartWidth + delta, 60f, maxLeft );
					Repaint();
					e.Use();
				}
				else if ( m_draggingRight )
				{
					float maxRight = position.width - m_leftW - panelDivW * 2 - 100f;
					m_middleW = Mathf.Clamp( m_dragStartWidth + delta, 80f, maxRight );
					Repaint();
					e.Use();
				}
			}
			else if ( e.type == EventType.MouseUp && e.button == 0 )
			{
				m_draggingLeft  = false;
				m_draggingRight = false;
			}
		}

		// ── Nav panel ──────────────────────────────────────────────────────────

		private void DrawNavPanel()
		{
			string[]  labels   = { "Home", "Installed", "Catalog", "Settings" };
			Section[] sections = { Section.Home, Section.Installed, Section.Catalog, Section.Settings };

			for ( int i = 0; i < labels.Length; i++ )
			{
				DrawNavItem( labels[ i ], sections[ i ] );
			}

			GUILayout.FlexibleSpace();

			// Logo placeholder
			{
				const float margin   = 8f;
				const float logoSize = 100f;
				var   raw    = GUILayoutUtility.GetRect( 0, logoSize + margin, GUIStyle.none, GUILayout.ExpandWidth( true ) );
				float cx     = raw.x + raw.width * 0.5f;
				var   logo   = new Rect( cx - logoSize * 0.5f, raw.y + margin * 0.5f - 10f, logoSize, logoSize );
				DrawPlaceholderRect( logo, ColorIconBorder, ColorIconPlaceholder );
				GUI.Label( logo, "company logo", m_iconLabelStyle );
				if ( Event.current.type == EventType.MouseDown && logo.Contains( Event.current.mousePosition ) )
				{
					Application.OpenURL( "https://assetstore.unity.com/publishers/707" );
					Event.current.Use();
				}
			}

			GUILayout.Space( 5 );

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				GUILayout.Label( "Show at Startup", EditorStyles.miniLabel );
				GUILayout.Space( 3 );
				bool current = ShowAtStartup;
				bool updated = EditorGUILayout.Toggle( current, GUILayout.Width( 14 ) );
				if ( updated != current ) ShowAtStartup = updated;
				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 13 );
		}

		private void DrawNavItem( string label, Section section )
		{
			bool active  = m_section == section;
			var  rect    = GUILayoutUtility.GetRect( 0, NavItemH, GUIStyle.none, GUILayout.ExpandWidth( true ) );
			bool hovered = !active && rect.Contains( Event.current.mousePosition );

			if ( active )
			{
				EditorGUI.DrawRect( rect, ColorNavActiveBg );
			}
			else if ( hovered )
			{
				EditorGUI.DrawRect( rect, ColorNavHoverBg );
			}

			EditorGUI.LabelField( rect, label, active ? m_navItemActiveStyle : m_navItemStyle );

			if ( Event.current.type == EventType.MouseDown && rect.Contains( Event.current.mousePosition ) )
			{
				m_section = section;
				if ( section == Section.Installed || section == Section.Catalog )
				{
					EnsureProductSelected();
				}
				Repaint();
			}
		}

		private void EnsureProductSelected()
		{
			if ( SelectedProduct != null ) return;
			var all      = Registry.AllProducts();
			var products = m_section == Section.Installed
				? new List<Product>( all ).FindAll( p => p.installed )
				: new List<Product>( all );
			if ( products.Count > 0 )
			{
				SelectedProduct = products[ 0 ];
			}
		}

		// ── Product list ───────────────────────────────────────────────────────

		private void DrawProductList()
		{
			m_productListScroll = EditorGUILayout.BeginScrollView( m_productListScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none );
			{
				var all      = Registry.AllProducts();
				var products = m_section == Section.Installed
					? new List<Product>( all ).FindAll( p => p.installed )
					: new List<Product>( all );

				for ( int i = 0; i < products.Count; i++ )
				{
					DrawProductListItem( products[ i ], i == 0, i == products.Count - 1 );
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private void DrawProductListItem( Product p, bool isFirst, bool isLast )
		{
			bool selected = SelectedProduct == p;
			var  rect     = GUILayoutUtility.GetRect( 0, ItemH, GUIStyle.none, GUILayout.ExpandWidth( true ) );
			bool hovered  = !selected && rect.Contains( Event.current.mousePosition );

			if ( selected )
			{
				EditorGUI.DrawRect( new Rect( 0f, rect.y, 99999f, rect.height ), ColorSelectedBg );
			}
			else if ( hovered )
			{
				EditorGUI.DrawRect( new Rect( 0f, rect.y, 99999f, rect.height ), ColorNavHoverBg );
			}

			if ( !isFirst )
			{
				EditorGUI.DrawRect( new Rect( 0f, rect.y, 99999f, DividerW ), ColorItemSeparator );
			}
			if ( isLast )
			{
				EditorGUI.DrawRect( new Rect( 0f, rect.yMax - DividerW, 99999f, DividerW ), ColorItemSeparator );
			}

			const float tagW    = 30f;
			const float verW    = 48f;
			const float padding = 6f;
			const float nameH   = 18f;
			const float descH   = 14f;
			const float padTop  = ( ItemH - nameH - descH ) * 0.5f;

			float right = padding;

			if ( p.installed && m_section == Section.Installed && !string.IsNullOrEmpty( p.version ) )
			{
				var verRect = new Rect( rect.xMax - right - verW, rect.y + padTop, verW, nameH );
				GUI.Label( verRect, p.version, m_versionStyle );
				right += verW;
			}

			var nameRect = new Rect( rect.x + padding, rect.y + padTop, rect.width - padding - right, nameH );
			EditorGUI.LabelField( nameRect, p.name, m_listItemStyle );

			bool showFreeTag = p.free && m_section != Section.Installed;
			if ( showFreeTag )
			{
				float descMidY  = rect.y + padTop + nameH + descH * 0.5f;
				float tagDrawW  = tagW  * 0.9f;
				float tagDrawH  = 13f   * 0.9f;
				var tagRect = new Rect( rect.xMax - padding - tagDrawW, descMidY - tagDrawH * 0.5f, tagDrawW, tagDrawH );
				EditorGUI.DrawRect( tagRect, ColorFreeTagBg );
				GUI.Label( tagRect, "FREE", m_tagLabelStyle );
			}

			if ( !string.IsNullOrEmpty( p.shortDescription ) )
			{
				float descRight = showFreeTag ? tagW + 4f + padding : padding;
				var descRect = new Rect( rect.x + padding, rect.y + padTop + nameH, rect.width - padding - descRight, descH );
				string descText = showFreeTag
					? TruncateWithEllipsis( p.shortDescription, m_listItemDescStyle, descRect.width )
					: p.shortDescription;
				EditorGUI.LabelField( descRect, descText, m_listItemDescStyle );
			}

			if ( Event.current.type == EventType.MouseDown && rect.Contains( Event.current.mousePosition ) )
			{
				SelectedProduct = p;
				Repaint();
			}
		}

		// ── Home banner ────────────────────────────────────────────────────────

		private void DrawHomeBanner( float areaW, float areaH )
		{
			const float marginX   = 20f;
			const float marginY   = 20f;
			const float bannerH   = 180f;
			const float gap       = 14f;
			const float btnW      = 120f;
			const float btnH      = 26f;
			const float cardH     = 280f;
			const float cardW     = 280f;
			const int   cardCols  = 3;
			const float cardGap   = 20f;

			float innerW  = areaW - 2f * marginX;

			// Total scrollable height: banner + gap + text block + buttons + gap + heading + gap + grid + bottom margin
			float textH      = 72f;
			float headingH   = 40f;
			var   allProducts = new List<Product>( Registry.AllProducts() );
			float gridRows   = Mathf.Ceil( (float)allProducts.Count / cardCols );
			float gridH      = gridRows * cardH + Mathf.Max( 0f, gridRows - 1f ) * cardGap;
			float rowH       = Mathf.Max( btnH, headingH );
			float totalH     = marginY + bannerH + gap + textH + rowH + gap + gridH + marginY;

			m_homeScroll = GUI.BeginScrollView( new Rect( 0, 0, areaW, areaH ), m_homeScroll, new Rect( 0, 0, areaW - 16f, totalH ), false, false );

			float y = marginY;

			// ── Banner ────────────────────────────────────────────────────────
			var bannerRect = new Rect( marginX, y, innerW, bannerH );
			DrawPlaceholderRect( bannerRect, ColorIconBorder, ColorIconPlaceholder );
			GUI.Label( bannerRect, "Banner", m_bannerLabelStyle );
			y += bannerH + gap;

			// ── Body text ─────────────────────────────────────────────────────
			var textRect = new Rect( marginX, y, innerW, textH );
			GUI.Label( textRect,
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et " +
				"dolore magna aliqua. Quis ipsum suspendisse ultrices gravida. Risus commodo viverra maecenas accumsan lacus " +
				"vel facilisis. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut " +
				"labore et dolore magna aliqua. Quis ipsum suspendisse ultrices gravida. Risus commodo viverra maecenas " +
				"accumsan lacus vel facilisis. Lorem ipsum dolor sit amet, consectetur adipi-",
				m_homeBodyStyle );
			y += textH;

			// ── Section heading + Buttons on same row ────────────────────────
			float headingY = y + ( rowH - headingH ) * 0.5f;
			GUI.Label( new Rect( marginX, headingY, innerW, headingH ), "Let's get started!", m_homeSectionTitleStyle );

			float btnY  = y + ( rowH - btnH ) * 0.5f;
			float btn2X = marginX + innerW - btnW;
			float btn1X = btn2X - btnW - 12f;
			if ( GUI.Button( new Rect( btn1X, btnY, btnW, btnH ), "Link" ) ) {}
			if ( GUI.Button( new Rect( btn2X, btnY, btnW, btnH ), "Link" ) ) {}
			y += rowH + gap;

			// ── Product grid ──────────────────────────────────────────────────
			for ( int i = 0; i < allProducts.Count; i++ )
			{
				int   col    = i % cardCols;
				int   row    = i / cardCols;
				float cardX  = marginX + col * ( cardW + cardGap );
				float cardY  = y + row * ( cardH + cardGap );
				var   card   = new Rect( cardX, cardY, cardW, cardH );

				DrawPlaceholderRect( card, ColorIconBorder, ColorIconPlaceholder );

				float padX = 20f, padY = 20f, lineH = 34f;
				GUI.Label( new Rect( card.x + padX, card.y + padY,               card.width - padX * 2, lineH * 1.2f ), allProducts[ i ].name,   m_homeProductTitleStyle );
				GUI.Label( new Rect( card.x + padX, card.y + padY + lineH * 1.5f, card.width - padX * 2, lineH ),        "buttons",               m_homeProductBodyStyle );
				GUI.Label( new Rect( card.x + padX, card.y + padY + lineH * 2.5f, card.width - padX * 2, lineH ),        "text",                  m_homeProductBodyStyle );
				GUI.Label( new Rect( card.x + padX, card.y + padY + lineH * 3.5f, card.width - padX * 2, lineH ),        "links",                 m_homeProductBodyStyle );
			}

			GUI.EndScrollView();
		}

		private List<Product> GetInstalledProducts()
		{
			var result = new List<Product>();
			foreach ( var p in Registry.AllProducts() )
			{
				if ( p.installed ) result.Add( p );
			}
			return result;
		}

		// ── Settings panel ─────────────────────────────────────────────────────

		private void DrawSettingsPanel()
		{
			GUILayout.Space( 20 );

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( 20 );
				GUILayout.Label( "Settings", m_settingsHeaderStyle );
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 12 );

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( 20 );
				EditorGUILayout.BeginVertical();
				{
					GUILayout.Label( "General", EditorStyles.boldLabel );
					GUILayout.Space( 6 );

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label( "Show at startup", GUILayout.Width( 200 ) );
						bool current = ShowAtStartup;
						bool updated = EditorGUILayout.Toggle( current );
						if ( updated != current ) ShowAtStartup = updated;
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label( "Check for updates on launch", GUILayout.Width( 200 ) );
						EditorGUILayout.Toggle( false );
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label( "Show pre-release versions", GUILayout.Width( 200 ) );
						EditorGUILayout.Toggle( false );
					}
					EditorGUILayout.EndHorizontal();

					GUILayout.Space( 16 );
					GUILayout.Label( "Catalog", EditorStyles.boldLabel );
					GUILayout.Space( 6 );

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label( "Show free products", GUILayout.Width( 200 ) );
						EditorGUILayout.Toggle( true );
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label( "Show paid products", GUILayout.Width( 200 ) );
						EditorGUILayout.Toggle( true );
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
				GUILayout.Space( 20 );
			}
			EditorGUILayout.EndHorizontal();
		}

		// ── Content panel ──────────────────────────────────────────────────────

		private void DrawContentPanel()
		{
			DrawTabs();

			bool hasContent = m_activeTab == Tab.Description && SelectedProduct != null;

			if ( hasContent )
			{
				m_contentScroll = EditorGUILayout.BeginScrollView( m_contentScroll );
				DrawDescription();
				EditorGUILayout.EndScrollView();
			}
			else
			{
				switch ( m_activeTab )
				{
					case Tab.Description: DrawPlaceholder( "Select a product from the list to view its details." ); break;
					case Tab.Wiki:        DrawPlaceholder( "Wiki content is not yet available." ); break;
					case Tab.Samples:     DrawPlaceholder( "No samples are available for this product." ); break;
					case Tab.Tutorials:   DrawPlaceholder( "No tutorials are available for this product." ); break;
				}
			}
		}

		private void DrawTabs()
		{
			string[] labels = { "Description", "Wiki", "Samples", "Tutorials" };
			Tab[]    tabs   = { Tab.Description, Tab.Wiki, Tab.Samples, Tab.Tutorials };

			var rowRect = GUILayoutUtility.GetRect( 0, TabH, GUIStyle.none, GUILayout.ExpandWidth( true ) );

			// Small adjustment to tab rect
			rowRect.x -= 2;
			rowRect.y -= 2;

			// Natural tab widths from label content + style padding
			var widths = new float[ labels.Length ];
			for ( int i = 0; i < labels.Length; i++ )
			{
				widths[ i ] = Mathf.Ceil( m_tabStyle.CalcSize( new GUIContent( labels[ i ] ) ).x );
			}

			// Locate the active tab's x range so we can leave a gap in the bottom border
			float activeX    = rowRect.x;
			float activeXMax = rowRect.x;
			{
				float xi = rowRect.x;
				for ( int i = 0; i < labels.Length; i++ )
				{
					if ( m_activeTab == tabs[ i ] )
					{
						activeX    = xi;
						activeXMax = xi + widths[ i ];
						break;
					}
					xi += widths[ i ];
				}
			}

			// Draw each tab background and borders first
			float x = rowRect.x;
			for ( int i = 0; i < labels.Length; i++ )
			{
				bool active  = m_activeTab == tabs[ i ];
				var  tabRect = new Rect( x, rowRect.y, widths[ i ], TabH );
				bool hovered = !active && tabRect.Contains( Event.current.mousePosition );

				// Inactive background (active shows through to window bg)
				if ( !active )
				{
					EditorGUI.DrawRect( tabRect, hovered ? ColorNavHoverBg : ColorTabInactiveBg );
				}

				// Top border
				EditorGUI.DrawRect( new Rect( tabRect.x, tabRect.y, tabRect.width, DividerW ), ColorDivider );
				// Left border on every tab (first tab's left aligns with the vertical panel divider)
				EditorGUI.DrawRect( new Rect( tabRect.x, tabRect.y, DividerW, TabH ), ColorDivider );
				// Right border only on the last tab
				if ( i == labels.Length - 1 )
				{
					EditorGUI.DrawRect( new Rect( tabRect.xMax - DividerW, tabRect.y, DividerW, TabH ), ColorDivider );
				}

				GUI.Label( tabRect, labels[ i ], active ? m_tabActiveStyle : m_tabStyle );

				if ( Event.current.type == EventType.MouseDown && tabRect.Contains( Event.current.mousePosition ) )
				{
					m_activeTab = tabs[ i ];
					Event.current.Use();
					Repaint();
				}

				x += widths[ i ];
			}

			// Bottom border across the full row width, with a gap where the active tab sits
			float bottomY = rowRect.yMax - DividerW;
			if ( activeX > rowRect.x )
			{
				EditorGUI.DrawRect( new Rect( rowRect.x, bottomY, activeX - rowRect.x, DividerW ), ColorDivider );
			}
			if ( activeXMax < rowRect.xMax )
			{
				EditorGUI.DrawRect( new Rect( activeXMax, bottomY, rowRect.xMax - activeXMax, DividerW ), ColorDivider );
			}
		}

		private void DrawDescription()
		{
			GUILayout.Space( 14 );

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( 14 );

				var iconRect = GUILayoutUtility.GetRect( 96, 96, GUILayout.Width( 96 ), GUILayout.Height( 96 ) );
				DrawPlaceholderRect( iconRect, ColorIconBorder, ColorIconPlaceholder );
				GUI.Label( iconRect, "Icon", m_iconLabelStyle );

				GUILayout.Space( 14 );

				EditorGUILayout.BeginVertical();
				{
					GUILayout.Space( 6 );

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label( SelectedProduct.name, m_productTitleStyle );
						if ( SelectedProduct.installed && !string.IsNullOrEmpty( SelectedProduct.version ) )
						{
							GUILayout.Label( SelectedProduct.version, m_versionStyle, GUILayout.Width( 60 ) );
						}
					}
					EditorGUILayout.EndHorizontal();

					GUILayout.Space( 2 );
					GUILayout.Label( $"Availability: {( SelectedProduct.free ? "Free" : "Paid" )}", EditorStyles.miniLabel );
					GUILayout.Space( 6 );

					if ( GUILayout.Button( "View on Asset Store", GUILayout.Width( 150 ) ) )
					{
						Application.OpenURL( SelectedProduct.url );
					}
				}
				EditorGUILayout.EndVertical();

				GUILayout.Space( 14 );
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 16 );

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( 14 );
				GUILayout.Label(
					"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
					"incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
					"exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
					"dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
					EditorStyles.wordWrappedLabel
				);
				GUILayout.Space( 14 );
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawPlaceholder( string message )
		{
			GUILayout.FlexibleSpace();
			GUILayout.Label( message, EditorStyles.centeredGreyMiniLabel );
			GUILayout.FlexibleSpace();
		}

		// ── Utilities ──────────────────────────────────────────────────────────

		private static string TruncateWithEllipsis( string text, GUIStyle style, float maxWidth )
		{
			if ( string.IsNullOrEmpty( text ) ) return text;
			if ( style.CalcSize( new GUIContent( text ) ).x <= maxWidth ) return text;
			int lo = 0, hi = text.Length;
			while ( lo < hi )
			{
				int mid = ( lo + hi + 1 ) / 2;
				if ( style.CalcSize( new GUIContent( text.Substring( 0, mid ) + "..." ) ).x <= maxWidth )
					lo = mid;
				else
					hi = mid - 1;
			}
			return lo == 0 ? "..." : text.Substring( 0, lo ) + "...";
		}

		private static void DrawPlaceholderRect( Rect r, Color borderColor, Color fillColor )
		{
			float ppp  = EditorGUIUtility.pixelsPerPoint;
			int   px   = Mathf.RoundToInt( r.x    * ppp );
			int   py   = Mathf.RoundToInt( r.y    * ppp );
			int   pxMx = Mathf.RoundToInt( r.xMax * ppp );
			int   pyMx = Mathf.RoundToInt( r.yMax * ppp );
			var outer  = new Rect( px / ppp,       py / ppp,       ( pxMx - px ) / ppp,       ( pyMx - py ) / ppp );
			var inner  = new Rect( ( px + 1 ) / ppp, ( py + 1 ) / ppp, ( pxMx - px - 2 ) / ppp, ( pyMx - py - 2 ) / ppp );
			EditorGUI.DrawRect( outer, borderColor );
			EditorGUI.DrawRect( inner, fillColor );
		}
	}

	[InitializeOnLoad]
	static class WindowStartup
	{
		static WindowStartup()
		{
			EditorApplication.delayCall += () =>
			{
				if ( Window.ShowAtStartup )
				{
					Window.Open();
				}
			};
		}
	}
}
