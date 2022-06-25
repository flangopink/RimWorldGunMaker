using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;
using System.Text.RegularExpressions;

#pragma warning disable CS8600, CS8602, CS8603, CS8604 // Possible null reference my ass.

namespace RimworldGunMaker
{
    public partial class MainForm : Form
    {
        string baseWeapon = "BaseHumanMakeableGun";
        string baseBullet = "BaseBullet";

        string decl = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
        string comment = " Your Custom Weapon Name ";

        readonly XDocument def;
        XDocument defCopy;
        XElement gunPath, bulletPath;

        public MainForm()
        {
            def = new(
                //new XDeclaration("1.0", "utf-8", null),  // doesn't work
                new XElement("Defs", new XComment(comment),
                    new XElement("ThingDef", new XAttribute("ParentName", baseWeapon)),
                    new XComment(""),
                    new XElement("ThingDef", new XAttribute("ParentName", baseBullet))
                ));

            // Make default NUD decimal separators dots instead of commas.
            var culture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            InitializeComponent();

            gunPath = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']");
            bulletPath = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']");

            defCopy = new(def);

            UpdateString();
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Brush _textBrush;

            // Get the item from the collection.
            TabPage _tabPage = MainTabControl.TabPages[e.Index];

            // Get the real bounds for the tab rectangle.
            Rectangle _tabBounds = MainTabControl.GetTabRect(e.Index);

            if (e.State == DrawItemState.Selected)
            {
                // Draw a different background color, and don't paint a focus rectangle.
                _textBrush = new SolidBrush(Color.White);
                g.FillRectangle(Brushes.DimGray, e.Bounds);
            }
            else
            {
                _textBrush = new SolidBrush(e.ForeColor);
                e.DrawBackground();
            }

            // Use our own font.
            Font _tabFont = new("SegoeUI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);

            // Draw string. Center the text.
            StringFormat _stringFlags = new();
            _stringFlags.Alignment = StringAlignment.Center;
            _stringFlags.LineAlignment = StringAlignment.Center;
            g.DrawString(_tabPage.Text, _tabFont, _textBrush, _tabBounds, new StringFormat(_stringFlags));
        }

        void UpdateString()
        {
            if (!chb_showDefs.Checked) 
            {
               var str = decl + Regex.Replace(XDocument.Parse(def.ToString()).ToString(), "<Defs>|</Defs>", string.Empty);
               rtb_output.Text = Regex.Replace(str, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline).Replace("<!---->", "");
            }
            else rtb_output.Text = decl + XDocument.Parse(def.ToString()).ToString().Replace("<!---->", "");
        }

        private static XElement ElementAtPath(XElement root, string path)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path.");
            
            return root.XPathSelectElement(path);
        }

        private void DefNameOrPrefix_Changed(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tb_defName.Text)) tb_defName.BackColor = char.IsDigit(tb_defName.Text[^1]) ? Color.Red : SystemColors.Window;
            UpdateDef();
            UpdateString();
        }

        void UpdateDef()
        {
            baseWeapon = chb_isCraftable.Checked ? "BaseHumanMakeableGun" : "BaseGunWithQuality";
            def.Root.Element("ThingDef").Attribute("ParentName").Value = baseWeapon;

            var value = tb_defName.Text;
            var tag = "defName";
            var elem = gunPath.Element(tag);
            var elem2 = bulletPath.Element(tag);
            var prefix = tb_prefix.Text;

            var defaultProj = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/defaultProjectile");

            if (string.IsNullOrEmpty(value))
            {
                elem?.Remove();
                elem2?.Remove();

                if (defaultProj != null) defaultProj.Value = "";
            }
            else
            {
                if (elem != null)
                    elem.Value = (string.IsNullOrEmpty(prefix) ? "Gun_" : prefix + "_Gun_") + value;
                else gunPath.AddFirst(new XElement(tag, (string.IsNullOrEmpty(prefix) ? "Gun_" : prefix + "_Gun_") + value));
                if (elem2 != null)
                    elem2.Value = (string.IsNullOrEmpty(prefix) ? "Bullet_" : prefix + "_Bullet_") + value;
                else bulletPath.AddFirst(new XElement(tag, (string.IsNullOrEmpty(prefix) ? "Bullet_" : prefix + "_Bullet_") + value));

                if (defaultProj != null) defaultProj.Value = elem2 == null ? "" : elem2.Value;
            }
        }

        private void Label_Changed(object sender, EventArgs e)
        {
            var tag = "label";
            var elem = gunPath.Element(tag);
            var elem2 = bulletPath.Element(tag);
            var value = (sender as TextBox).Text;
            var xcomment = def.Root.DescendantNodes().OfType<XComment>(); // i miss xcom

            if (string.IsNullOrEmpty(value))
            {
                elem?.Remove();
                elem2?.Remove();

                comment = " Your Custom Weapon Name ";
                if (xcomment.Any()) xcomment.First().Value = comment;
            }
            else
            {
                if (elem != null) elem.Value = value;
                else gunPath.Add(new XElement(tag, value));

                if (elem2 != null) elem2.Value = value + " bullet";
                else bulletPath.Add(new XElement(tag, value + " bullet"));

                comment = $" {value} ";
                if (xcomment.Any()) xcomment.First().Value = comment;
            }
            UpdateString();
        }

        private void Description_Changed(object sender, EventArgs e)
        {
            var tag = "description";
            var elem = gunPath.Element(tag);
            var value = (sender as RichTextBox).Text;

            if (string.IsNullOrEmpty(value))
            {
                elem?.Remove();
            }
            else
            {
                if (elem != null) elem.Value = value;
                else gunPath.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        private void Rtb_desc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                e.Handled = true;
        }

        private void TexPath_Changed(object sender, EventArgs e)
        {
            if (gunPath.Element("graphicData") == null)
                gunPath.Add(new XElement("graphicData", new XElement("texPath")));

            var parentElem = gunPath.Element("graphicData");

            var elem = parentElem.Element("texPath");
            var value = (sender as TextBox).Text;

            if (string.IsNullOrEmpty(value))
            {
                parentElem.Remove();
            }
            else
            {
                elem.Value = value;
                if (parentElem.Element("graphicClass") == null)
                    parentElem.Add(new XElement("graphicClass", "Graphic_Single"));
            }
            UpdateString();
        }

        private void BulletTex_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bulletPath.Element("graphicData") == null)
                bulletPath.Add(new XElement("graphicData", new XElement("texPath")));

            var parentElem = bulletPath.Element("graphicData");

            var elem = parentElem.Element("texPath");
            var value = sender.GetType().Name switch
            {
                "ComboBox" => "Things/Projectile/" + (sender as ComboBox).SelectedItem.ToString(),
                "TextBox" => (sender as ComboBox).Text,
                _ => ""
            };

            if (string.IsNullOrEmpty(value))
            {
                parentElem.Remove();
            }
            else
            {
                elem.Value = value;
                if (parentElem.Element("graphicClass") == null)
                    parentElem.Add(new XElement("graphicClass", "Graphic_Single"));
            }
            UpdateString();
        }

        private void TagOrClass_Changed(object sender, EventArgs e)
        {
            string controlName = (sender as Control).Name;

            string tag = controlName switch // thank you intellisense, very cool!
            {
                "cb_tag" => "weaponTags",
                "cb_class" => "weaponClasses",
                _ => "", // required because of CS0165
            };

            if (gunPath.Element(tag) == null)
                gunPath.Add(new XElement(tag, new XElement("li")));

            var parentElem = gunPath.Element(tag);

            var elem = parentElem.Element("li");
            var value = (sender as ComboBox).SelectedItem.ToString();

            if (string.IsNullOrEmpty(value) || value == "None") parentElem.Remove();
            else elem.Value = value;

            UpdateString();
        }

        private void InteractSound_Changed(object sender, EventArgs e)
        {
            var tag = "soundInteract";
            var elem = gunPath.Element(tag);
            var value = sender.GetType().Name switch
            {
                "ComboBox" => (sender as ComboBox).SelectedItem.ToString(),
                "TextBox" => (sender as TextBox).Text,
                _ => ""
            };

            if (string.IsNullOrEmpty(value))
            {
                elem?.Remove();
            }
            else
            {
                if (elem != null) elem.Value = value;
                else gunPath.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        void CheckStatBases()
        {
            var elem = gunPath.Element("statBases");

            if (elem == null)
            {
                gunPath.Add(new XElement("statBases"));
                return;
            }

            if (!elem.HasElements) elem.Remove();
        }

        private void StatBasesItem_Changed(object sender, EventArgs e)
        {
            CheckStatBases();

            var parentElem = gunPath.Element("statBases");
            var controlName = (sender as NumericUpDown).Name;
            var value = (sender as NumericUpDown).Value;

            string tag = controlName switch
            {
                "nud_workToMake" => "WorkToMake",
                "nud_mass" => "Mass",
                "nud_accTouch" => "AccuracyTouch",
                "nud_accShort" => "AccuracyShort",
                "nud_accMedium" => "AccuracyMedium",
                "nud_accLong" => "AccuracyLong",
                "nud_rangedCooldown" => "RangedWeapon_Cooldown",
                "nud_marketValue" => "MarketValue",
                _ => "",
            };

            var elem = parentElem.Element(tag);

            if (value == 0)
            {
                elem?.Remove();
                CheckStatBases(); // Check if <statBases> has no children
            }
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else parentElem.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        void CheckCostList()
        {
            var elem = gunPath.Element("costList");

            if (elem == null)
            {
                gunPath.Add(new XElement("costList"));
                return;
            }

            if (!elem.HasElements) elem.Remove();
        }

        private void CostListItem_Changed(object sender, EventArgs e)
        {
            CheckCostList();

            var parentElem = gunPath.Element("costList");
            var controlName = (sender as NumericUpDown).Name;
            var value = (sender as NumericUpDown).Value;

            string tag = controlName switch
            {
                "nud_steel" => "Steel",
                "nud_comp" => "ComponentIndustrial",
                "nud_plasteel" => "Plasteel",
                "nud_advcomp" => "ComponentSpacer",
                "nud_wood" => "WoodLog",
                "nud_chemfuel" => "Chemfuel",
                _ => "",
            };

            var elem = parentElem.Element(tag);

            if (value == 0)
            {
                elem?.Remove();
                CheckCostList(); // Check if <statBases> has no children
            }
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else parentElem.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        void UpdatePaths()
        {
            def.Root.Descendants("ThingDef").First().Attribute("ParentName").Value = baseWeapon;
            def.Root.Descendants("ThingDef").Last().Attribute("ParentName").Value = baseBullet;

            gunPath = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']");
            bulletPath = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']");

            UpdateString();
        }

        private void TechLevel_Changed(object sender, EventArgs e)
        {
            var tag = "techLevel";
            var value = (sender as ComboBox).SelectedItem.ToString();

            if (value is "Neolithic" or "Medieval")
            {
                baseWeapon = "BaseWeaponNeolithic";
                baseBullet = "BaseProjectileNeolithic";
            }
            else
            {
                baseWeapon = "BaseHumanMakeableGun";
                baseBullet = "BaseBullet";
            }
            UpdatePaths();

            var elem = gunPath.Element(tag);

            if (string.IsNullOrEmpty(value))
            {
                elem?.Remove();
            }
            else
            {
                if (elem != null) elem.Value = value;
                else gunPath.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        void CheckRecipeMaker()
        {
            var elem = gunPath.Element("recipeMaker");

            if (elem == null)
            {
                gunPath.Add(new XElement("recipeMaker"));
                return;
            }

            if (!elem.HasElements) elem.Remove();
        }

        private void RecipeMakerItem_Changed(object sender, EventArgs e)
        {
            CheckRecipeMaker();

            var parentElem = gunPath.Element("recipeMaker");
            string parentTag = "";
            string tag;
            string controlName;
            string value;

            if (sender is NumericUpDown)
            {
                controlName = (sender as NumericUpDown).Name;
                value = (sender as NumericUpDown).Value.ToString();
            }
            else
            {
                controlName = (sender as ComboBox).Name;
                value = (sender as ComboBox).SelectedItem.ToString();
            }

            switch (controlName) // im surprised this works
            {
                case "nud_reqSkill":
                    parentTag = "skillRequirements";
                    tag = "Crafting";
                    if(parentElem.Element(parentTag) == null)
                        parentElem.Add(new XElement(parentTag));
                    break;

                case "cb_workbench":
                    parentTag = "recipeUsers";
                    tag = "li";
                    if (parentElem.Element(parentTag) == null)
                        parentElem.Add(new XElement(parentTag));
                    else
                        parentElem.Element(parentTag).RemoveAll();
                    break;

                case "cb_reqResearch":
                    tag = "researchPrerequisite";
                    break;

                default:
                    return;
            }

            var elem = controlName == "cb_reqResearch" ? parentElem.Element(tag) : parentElem.Element(parentTag).Element(tag);

            if ((int.TryParse(value, out int i) && i == 0 ) || string.IsNullOrEmpty(value))
            {
                if (controlName == "cb_reqResearch") elem?.Remove();
                else parentElem.Element(parentTag).Remove();

                CheckRecipeMaker(); // Check if <statBases> has no children
            }
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else
                {
                    if (controlName == "cb_reqResearch")
                        parentElem.Add(new XElement(tag, value));
                    else
                    {
                        if (value == "Smithy")
                            parentElem.Element(parentTag).Add(new XElement(tag, "FueledSmithy"),
                                                              new XElement(tag, "ElectricSmithy"));
                        else parentElem.Element(parentTag).Add(new XElement(tag, value));
                    }
                }
            }
            UpdateString();
        }

        private void IsCraftable_Changed(object sender, EventArgs e)
        {
            List<Control> controls = new() { l_cr1, l_cr2, l_cr3, l_cr4, l_cr5, l_cr6, l_cr7, cb_techLevel, cb_reqResearch, cb_workbench, nud_reqSkill, nud_steel, nud_plasteel, nud_comp, nud_advcomp, nud_wood, nud_chemfuel, nud_workToMake};

            foreach (Control c in controls)
                c.Enabled = (sender as CheckBox).Checked;

            // Heresy starts here
            if (!(sender as CheckBox).Checked)
                defCopy = new(XDocument.Parse(def.ToString()));

            XElement cl2 = null, rm2 = null, tl2 = null, wtm2 = null; // Is it really that necessary?

            var cl = gunPath.Element("costList");
            var rm = gunPath.Element("recipeMaker");
            var tl = gunPath.Element("techLevel");
            var wtm = ElementAtPath(def.Root, $"ThingDef[@ParentName='BaseHumanMakeableGun']/statBases/WorkToMake");

            // Copy
            if ((sender as CheckBox).Checked)
            {
                var gunPath2 = new XElement(ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='BaseHumanMakeableGun']"));
                if (gunPath2.Element("costList") != null) cl2 = new XElement(gunPath2.Element("costList"));
                if (gunPath2.Element("recipeMaker") != null) rm2 = new XElement(gunPath2.Element("recipeMaker"));
                if (gunPath2.Element("techLevel") != null) tl2 = new XElement(gunPath2.Element("techLevel"));
                if (ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='BaseHumanMakeableGun']/statBases/WorkToMake") != null) wtm2 = new XElement(ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='BaseHumanMakeableGun']/statBases/WorkToMake"));
            }

            if (!(sender as CheckBox).Checked)
            {
                cl?.Remove(); rm?.Remove(); tl?.Remove(); wtm?.Remove();
            }
            else
            {   // Paste
                if (cl2 != null) gunPath.Add(new XElement(cl2));
                if (rm2 != null) gunPath.Add(new XElement(rm2));
                if (tl2 != null) gunPath.Add(new XElement(tl2));
                if (wtm2 != null) gunPath.Add(new XElement(wtm2));
            }
            UpdateDef();
            UpdateString();
        }

        void CheckTools()
        {
            var elem = gunPath.Element("tools");

            if (elem == null)
            {
                gunPath.Add(new XElement("tools"));
                return;
            }
            if (!elem.HasElements) elem.Remove();
        }

        void HideShowPart(object sender, string partName)
        {
            XElement li = null, li2 = null;

            var toolsElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools");

            if (toolsElem.Descendants("li").Any())
                li = toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
            
            // Copy
            if (li != null) li2 = new XElement(toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).First());

            if (!(sender as CheckBox).Checked)
            {   // "Hide"
                li?.Remove();
                CheckTools();
            }
            else // Paste
                if (li2 != null && li == null) toolsElem.Add(new XElement(li2));
        }

        private void CheckSwitch(object sender, EventArgs e)
        {
            var cb = (CheckBox) sender;
            var controlName = cb.Name;
            string partName;

            if (!(sender as CheckBox).Checked)
                defCopy = new(XDocument.Parse(def.ToString()));

            if (controlName is not ("chb_isBurst" or "chb_isIncendiary")) CheckTools();

            var toolsElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools");

            switch (controlName) 
            {
                #region - Burst -
                case "chb_isBurst":
                    nud_burstCount.Enabled = nud_burstDelay.Enabled = l_b1.Enabled = l_b2.Enabled = cb.Checked;
                    
                    XElement bc2 = null, bd2 = null;

                    var bc = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/burstShotCount");
                    var bd = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/ticksBetweenBurstShots");

                    // Copy
                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/burstShotCount") != null) bc2 = new XElement(ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/burstShotCount"));
                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/ticksBetweenBurstShots") != null) bd2 = new XElement(ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/ticksBetweenBurstShots"));  // https://youtu.be/tnqVqZyvLes

                    if (!(sender as CheckBox).Checked)
                    {   // "Hide"
                        bc?.Remove();
                        bd?.Remove();
                    }
                    else
                    {   // Paste
                        if (bc2 != null) gunPath.Add(new XElement(bc2));
                        if (bd2 != null) gunPath.Add(new XElement(bd2));
                    }
                    UpdateString();

                    break;
                #endregion

                #region - Melee -
                case "chb_hasBarrel":
                    nud_barrelDamage.Enabled = nud_barrelCooldown.Enabled = l_m1.Enabled = l_m2.Enabled = cb.Checked;

                    partName = "barrel";

                    if (toolsElem != null)
                    {
                        if (!toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            toolsElem.Add(new XElement("li",
                                                new XElement("label", partName),
                                                new XElement("capacities", new XElement("li", "Blunt"),
                                                                           new XElement("li", "Poke")),
                                                new XElement("power", nud_barrelDamage.Value),
                                                new XElement("cooldownTime", nud_barrelCooldown.Value)));
                        }
                    }

                    HideShowPart(sender, partName);
                    UpdateString();
                    break;

                case "chb_hasStock":
                    nud_stockDamage.Enabled = nud_stockCooldown.Enabled = l_m3.Enabled = l_m4.Enabled = cb.Checked;

                    partName = "stock";

                    if (toolsElem != null)
                    {
                        if (!toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            toolsElem.Add(new XElement("li",
                                                new XElement("label", partName),
                                                new XElement("capacities", new XElement("li", "Blunt")),
                                                new XElement("power", nud_stockDamage.Value),
                                                new XElement("cooldownTime", nud_stockCooldown.Value)));
                        }
                    }

                    HideShowPart(sender, partName);
                    UpdateString();
                    break;

                case "chb_hasGrip":
                    nud_gripDamage.Enabled = nud_gripCooldown.Enabled = l_m5.Enabled = l_m6.Enabled = cb.Checked;

                    partName = "grip";

                    if (toolsElem != null)
                    {
                        if (!toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            toolsElem.Add(new XElement("li",
                                                new XElement("label", partName),
                                                new XElement("capacities", new XElement("li", "Blunt")),
                                                new XElement("power", nud_gripDamage.Value),
                                                new XElement("cooldownTime", nud_gripCooldown.Value)));
                        }
                    }

                    HideShowPart(sender, partName);
                    UpdateString(); 
                    break;

                case "chb_hasBlade":
                    nud_bladeDamage.Enabled = nud_bladeCooldown.Enabled = l_m7.Enabled = l_m8.Enabled = cb.Checked;

                    partName = "blade";

                    if (toolsElem != null)
                    {
                        if (!toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            toolsElem.Add(new XElement("li",
                                                new XElement("label", partName),
                                                new XElement("capacities", new XElement("li", "Cut"),
                                                                           new XElement("li", "Stab")),
                                                new XElement("power", nud_bladeDamage.Value),
                                                new XElement("cooldownTime", nud_bladeCooldown.Value)));
                        }
                    }

                    HideShowPart(sender, partName);
                    UpdateString(); 
                    break;

                case "chb_hasLimb":
                    nud_limbDamage.Enabled = nud_limbCooldown.Enabled = l_m9.Enabled = l_m10.Enabled = cb.Checked;

                    partName = "limb";

                    if (toolsElem != null)
                    {
                        if (!toolsElem.Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            toolsElem.Add(new XElement("li",
                                                new XElement("label", partName),
                                                new XElement("capacities", new XElement("li", "Blunt"),
                                                                           new XElement("li", "Poke")),
                                                new XElement("power", nud_limbDamage.Value),
                                                new XElement("cooldownTime", nud_limbCooldown.Value)));
                        }
                    }

                    HideShowPart(sender, partName);
                    UpdateString(); 
                    break;

                #endregion

                case "chb_isIncendiary":
                    var parent = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/projectile");
                    var elem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/projectile/ai_IsIncendiary");
                    var value = chb_isIncendiary.Checked.ToString();

                    if (elem != null) elem.Value = value;
                    else parent.Add(new XElement("ai_IsIncendiary", value));

                    UpdateString();
                    break;
            }
        }

        void DamageOrCooldown(string partName, NumericUpDown nud, string dmg)
        {
            var elem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();

            if (nud.Name == dmg) elem.Element("power").Value = nud.Value.ToString();
            else elem.Element("cooldownTime").Value = nud.Value.ToString();
        }

        private void MeleeDmgCD_Changed(object sender, EventArgs e)
        {
            var nud = (NumericUpDown)sender;

            switch (nud.Name)
            {
                case "nud_barrelDamage" or "nud_barrelCooldown":
                    DamageOrCooldown("barrel", nud, "nud_barrelDamage");
                    break;
                case "nud_stockDamage" or "nud_stockCooldown":
                    DamageOrCooldown("stock", nud, "nud_stockDamage");
                    break;
                case "nud_gripDamage" or "nud_gripCooldown":
                    DamageOrCooldown("grip", nud, "nud_gripDamage");
                    break;
                case "nud_bladeDamage" or "nud_bladeCooldown":
                    DamageOrCooldown("blade", nud, "nud_bladeDamage");
                    break;
                case "nud_limbDamage" or "nud_limbCooldown":
                    DamageOrCooldown("limb", nud, "nud_limbDamage");
                    break;
            }
            UpdateString();
        }

        void CheckVerbs()
        {
            var elem = gunPath.Element("verbs");

            if (elem == null)
            {
                gunPath.Add(new XElement("verbs",
                                new XElement("li",
                                    new XElement("verbClass", "Verb_Shoot"),
                                    new XElement("hasStandardCommand", "true"),
                                    new XElement("defaultProjectile", (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/defName") == null ? "" : ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/defName").Value) // fuck you
                            ))));
                return;
            }

            var defaultProj = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/defaultProjectile");
            var defName = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/defName");

            if (string.IsNullOrEmpty(defaultProj.Value))
                defaultProj.Value = defName == null ? "" : defName.Value;

            if (!elem.HasElements) elem.Remove();
        }

        private void VerbsItem_KeyPress(object sender, KeyPressEventArgs e)
        {
            CheckVerbs();
        }

        private void VerbsItem_Changed(object sender, EventArgs e)
        {
            CheckVerbs();

            var parentElem = gunPath.Element("verbs").Element("li");
            string controlName = ((Control)sender).Name;
            string value;
            if (controlName != "tb_customShotSound") value = sender is NumericUpDown nud ? nud.Value.ToString() : ((ComboBox)sender).SelectedItem.ToString();
            else value = (sender as TextBox).Text;

            string tag = controlName switch
            {
                "nud_rangedWarmup" => "warmupTime",
                "nud_range" => "range",
                "nud_burstCount" => "burstShotCount",
                "nud_burstDelay" => "ticksBetweenBurstShots",
                "cb_shotSound" or "tb_customShotSound" => "soundCast",
                "cb_shotTailSound" => "soundCastTail",
                "nud_muzzleflashScale" => "muzzleFlashScale",
                _ => "",
            };

            var elem = parentElem.Element(tag);

            if ((int.TryParse(value, out int i) && i == 0) || string.IsNullOrEmpty(value))
            {
                elem?.Remove();
                CheckVerbs(); // Check if <verbs> has no children
            }
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else parentElem.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        void CheckProjectile()
        {
            var elem = bulletPath.Element("projectile");

            if (elem == null)
            {
                bulletPath.Add(new XElement("projectile"));
                return;
            }

            if (!elem.HasElements) elem.Remove();
        }

        private void ProjectileItem_Changed(object sender, EventArgs e)
        {
            CheckProjectile();

            var parentElem = bulletPath.Element("projectile");
            string tag;

            string controlName = ((Control)sender).Name;

            var value = sender is NumericUpDown nud2 ? nud2.Value.ToString() : ((ComboBox)sender).SelectedItem.ToString();

            switch (controlName) // im surprised this works
            {
                case "nud_rangedDamage":
                    tag = "damageAmountBase";
                    break;
                case "nud_stoppingPower":
                    tag = "stoppingPower";
                    break;
                case "nud_armorPen":
                    tag = "armorPenetrationBase";
                    break;
                case "nud_bulletSpeed":
                    tag = "speed";
                    break;
                case "cb_damageDef":
                    tag = "damageDef";
                    bool bomb = cb_damageDef.SelectedItem.ToString() is "Bomb" or "EMP";
                    l_bomb1.Enabled = l_bomb2.Enabled = l_bomb3.Enabled 
                        = nud_explRadius.Enabled = nud_explDelay.Enabled = nud_arcHeight.Enabled 
                        = chb_isIncendiary.Enabled = bomb;
                    if (!bomb)
                    {
                        parentElem.Element("ai_IsIncendiary")?.Remove();
                        parentElem.Element("explosionRadius")?.Remove();
                        parentElem.Element("explosionDelay")?.Remove();
                        parentElem.Element("arcHeightFactor")?.Remove();
                    }
                    break;
                case "nud_explRadius":
                    tag = "explosionRadius";
                    break;
                case "nud_explDelay":
                    tag = "explosionDelay";
                    break;
                case "nud_arcHeight":
                    tag = "arcHeightFactor";
                    break;
                default:
                    return;
            }

            var elem = parentElem.Element(tag);

            if ((int.TryParse(value, out int i) && i == 0) || string.IsNullOrEmpty(value))
            {
                elem?.Remove();
                CheckProjectile(); // Check if <projectile> has no children
            }
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else parentElem.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        private void ToggleDeclaration(object sender, EventArgs e)
        {
            decl = chb_toggleDecl.Checked ? "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" : string.Empty;
            UpdateString();
        }

        private void ToggleHideDefs(object sender, EventArgs e)
        {
            UpdateString();
        }

        private void ToggleComment(object sender, EventArgs e)
        {
            if (chb_showComment.Checked)
                def.Root.AddFirst(new XComment(comment));
            else def.Root.DescendantNodes().OfType<XComment>().First().Remove();
            UpdateString();
        }

        private void TexValues_Changed(object sender, EventArgs e)
        {
            var controlName = (sender as NumericUpDown).Name;
            var value = (sender as NumericUpDown).Value;

            string tag = controlName switch
            {
                "nud_angleOffset" => "equippedAngleOffset",
                "nud_uiIconScale" => "uiIconScale",
                _ => "",
            };

            var elem = gunPath.Element(tag);

            if ((tag == "equippedAngleOffset" && value == 0) || (tag == "uiIconScale" && value == 1))
                elem?.Remove();
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else gunPath.Add(new XElement(tag, value));
            }
            UpdateString();
        }

        private void CustomStuff_Changed(object sender, EventArgs e)
        {
            cb_interactSound.Enabled = cb_interactSound.Visible = !chb_customInteract.Checked;
            tb_customInteractSound.Enabled = tb_customInteractSound.Visible = chb_customInteract.Checked;
            cb_shotSound.Enabled = cb_shotSound.Visible = !chb_customShotSound.Checked;
            tb_customShotSound.Enabled = tb_customShotSound.Visible = chb_customShotSound.Checked;
            cb_bulletTex.Enabled = cb_bulletTex.Visible = !chb_customBulletTex.Checked;
            tb_customBulletTex.Enabled = tb_customBulletTex.Visible = chb_customBulletTex.Checked;
        }

        private void IsOversized_Checked(object sender, EventArgs e)
        {
            l_ov1.Enabled = l_ov2.Enabled = l_ov3.Enabled = l_ov4.Enabled = l_ov5.Enabled = l_ov6.Enabled
            = nud_drawSize.Enabled = tb_ovE.Enabled = tb_ovN.Enabled = tb_ovS.Enabled = tb_ovW.Enabled = chb_isOversized.Checked;

            if (gunPath.Element("comps") == null)
                gunPath.Add(new XElement("comps",
                                new XElement("li",
                                    new XElement("compClass", "CompOversizedWeapon.CompOversizedWeapon"))));
            else gunPath.Element("comps").Remove();
            UpdateString();
        }

        private void OversizedValues_Changed(object sender, EventArgs e)
        {
            if (gunPath.Element("graphicData") == null)
                gunPath.Add(new XElement("graphicData"));

            var parentElem = gunPath.Element("graphicData");

            var controlName = ((Control)sender).Name;
            string value = sender is NumericUpDown nud ? nud.Value.ToString() : (sender as MaskedTextBox).Text;
            string tag = controlName switch
            {
                "nud_drawSize" => "drawSize",
                "tb_ovN" => "drawOffsetNorth",
                "tb_ovE" => "drawOffsetEast",
                "tb_ovS" => "drawOffsetSouth",
                "tb_ovW" => "drawOffsetWest",
                _ => "",
            };
            var elem = parentElem.Element(tag);

            if ((decimal.TryParse(value, out decimal i) && i == 1) || string.IsNullOrEmpty(value) || value == "0.0, 0.0, 0.0" || value == " . ,  . ,  .")
            {
                elem?.Remove();
            }
            else
            {
                if (elem != null) elem.Value = value.ToString();
                else parentElem.Add(new XElement(tag, value));
            }

            if (!parentElem.HasElements) parentElem.Remove();

            UpdateString();
        }
    }
}