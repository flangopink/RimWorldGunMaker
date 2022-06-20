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
                    new XElement("ThingDef", new XAttribute("ParentName", baseBullet))
                )
            );

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

        void UpdateString()
        {
            if (!chb_showDefs.Checked) 
            {
               var str = decl + Regex.Replace(XDocument.Parse(def.ToString()).ToString(), "<Defs>|</Defs>", string.Empty);
               rtb_output.Text = Regex.Replace(str, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            }
            else rtb_output.Text = decl + XDocument.Parse(def.ToString());
        }

        private static XElement ElementAtPath(XElement root, string path)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid path.");
            }
            return root.XPathSelectElement(path);
        }

        private void Tb_prefix_TextChanged(object sender, EventArgs e)
        {
            UpdateDef();
            UpdateString();
        }

        private void Tb_defName_TextChanged(object sender, EventArgs e)
        {
            UpdateDef();
            UpdateString();
        }

        void UpdateDef()
        {
            var value = tb_defName.Text;
            var tag = "defName";
            var elem = gunPath.Element(tag);
            var elem2 = bulletPath.Element(tag);
            var prefix = tb_prefix.Text;

            if (string.IsNullOrEmpty(value))
            {
                if (elem != null) elem.Remove();
                if (elem2 != null) elem2.Remove();
            }
            else
            {
                if (elem != null)
                    elem.Value = (string.IsNullOrEmpty(prefix) ? "Gun_" : prefix + "_Gun_") + value;
                else gunPath.AddFirst(new XElement(tag, (string.IsNullOrEmpty(prefix) ? "Gun_" : prefix + "_Gun_") + value));
                if (elem2 != null)
                    elem2.Value = (string.IsNullOrEmpty(prefix) ? "Bullet_" : prefix + "_Bullet_") + value;
                else bulletPath.AddFirst(new XElement(tag, (string.IsNullOrEmpty(prefix) ? "Bullet_" : prefix + "_Bullet_") + value));
            }
        }

        private void Label_Changed(object sender, EventArgs e)
        {
            var tag = "label";
            var elem = gunPath.Element(tag);
            var elem2 = bulletPath.Element(tag);
            var value = (sender as TextBox).Text;

            if (string.IsNullOrEmpty(value))
            {
                if (elem != null) elem.Remove();
                if (elem2 != null) elem2.Remove();

                comment = " Your Custom Weapon Name ";
                if (def.Root.DescendantNodes().OfType<XComment>().Any()) 
                    def.Root.DescendantNodes().OfType<XComment>().First().Value = comment;
            }
            else
            {
                if (elem != null) elem.Value = value;
                else gunPath.Add(new XElement(tag, value));
                if (elem2 != null) elem2.Value = value + " bullet";
                else bulletPath.Add(new XElement(tag, value + " bullet"));

                comment = $" {value} ";
                if (def.Root.DescendantNodes().OfType<XComment>().Any()) 
                    def.Root.DescendantNodes().OfType<XComment>().First().Value = comment;
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
                if (elem != null) elem.Remove();
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
            var value = "Things/Projectile/" + (sender as ComboBox).SelectedItem.ToString();

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

        private void Tag_Changed(object sender, EventArgs e)
        {
            var tag = "weaponTags";

            if (gunPath.Element(tag) == null)
                gunPath.Add(new XElement(tag, new XElement("li")));

            var parentElem = gunPath.Element(tag);

            var elem = parentElem.Element("li");
            var value = (sender as ComboBox).SelectedItem.ToString();

            if (string.IsNullOrEmpty(value) || value == "None") parentElem.Remove();
            else elem.Value = value;

            UpdateString();
        }

        private void Class_Changed(object sender, EventArgs e)
        {
            var tag = "weaponClasses";

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
            var value = (sender as ComboBox).SelectedItem.ToString();

            if (string.IsNullOrEmpty(value))
            {
                if (elem != null) elem.Remove();
            }
            else
            {
                if (elem != null) elem.Value = value.ToLower();
                else gunPath.Add(new XElement(tag, value.ToLower()));
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

            if (!elem.Nodes().Any()) elem.Remove();
        }

        private void StatBasesItem_Changed(object sender, EventArgs e)
        {
            CheckStatBases();

            var parentElem = gunPath.Element("statBases");

            string tag;

            var controlName = (sender as NumericUpDown).Name;
            var value = (sender as NumericUpDown).Value;

            switch (controlName) // im surprised this works
            {
                case "nud_workToMake":
                    tag = "WorkToMake";
                    break;
                case "nud_mass":
                    tag = "Mass";
                    break;
                case "nud_accTouch":
                    tag = "AccuracyTouch";
                    break;
                case "nud_accShort":
                    tag = "AccuracyShort";
                    break;
                case "nud_accMedium":
                    tag = "AccuracyMedium";
                    break;
                case "nud_accLong":
                    tag = "AccuracyLong";
                    break;
                case "nud_rangedCooldown":
                    tag = "RangedWeapon_Cooldown";
                    break;
                case "nud_marketValue":
                    tag = "MarketValue";
                    break;
                default:
                    return;
            }

            var elem = parentElem.Element(tag);

            if (value == 0)
            {
                if (elem != null) elem.Remove();
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

            if (!elem.Nodes().Any()) elem.Remove();
        }

        private void CostListItem_Changed(object sender, EventArgs e)
        {
            CheckCostList();

            var parentElem = gunPath.Element("costList");

            string tag;

            var controlName = (sender as NumericUpDown).Name;
            var value = (sender as NumericUpDown).Value;

            switch (controlName) // im surprised this works
            {
                case "nud_steel":
                    tag = "Steel";
                    break;
                case "nud_comp":
                    tag = "ComponentIndustrial";
                    break;
                case "nud_plasteel":
                    tag = "Plasteel";
                    break;
                case "nud_advcomp":
                    tag = "ComponentSpacer";
                    break;
                case "nud_wood":
                    tag = "WoodLog";
                    break;
                case "nud_chemfuel":
                    tag = "Chemfuel";
                    break;
                default:
                    return;
            }

            var elem = parentElem.Element(tag);

            if (value == 0)
            {
                if (elem != null) elem.Remove();
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
                if (elem != null) elem.Remove();
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

            if (!elem.Nodes().Any()) elem.Remove();
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
                if (controlName == "cb_reqResearch")
                    if (elem != null) elem.Remove();
                else
                    parentElem.Element(parentTag).Remove();
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
                        {
                            parentElem.Element(parentTag).Add(new XElement(tag, "FueledSmithy"),
                                                              new XElement(tag, "ElectricSmithy"));
                        }
                        else
                        {
                            parentElem.Element(parentTag).Add(new XElement(tag, value));
                        }
                    }
                }
            }

            UpdateString();
        }

        private void IsCraftable_Changed(object sender, EventArgs e)
        {
            List<Control> controls = new() { l_cr1, l_cr2, l_cr3, l_cr4, l_cr5, l_cr6, l_cr7,
                                            cb_techLevel, cb_reqResearch, cb_workbench,
                                            nud_reqSkill, nud_steel, nud_plasteel, nud_comp,
                                            nud_advcomp, nud_wood, nud_chemfuel, nud_workToMake};

            foreach (Control c in controls)
                c.Enabled = (sender as CheckBox).Checked;

            // Heresy starts here

            if (!(sender as CheckBox).Checked)
                defCopy = new(XDocument.Parse(def.ToString()));

            XElement cl2 = null, rm2 = null, tl2 = null, wtm2 = null; // Is it really that necessary?
           
            var cl = gunPath.Element("costList");
            var rm = gunPath.Element("recipeMaker");
            var tl = gunPath.Element("techLevel");
            var wtm = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/statBases/WorkToMake");

            // Copy
            var gunPath2 = new XElement(ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='{baseWeapon}']"));
            if (gunPath2.Element("costList") != null) cl2 = new XElement(gunPath2.Element("costList"));
            if (gunPath2.Element("recipeMaker") != null) rm2 = new XElement(gunPath2.Element("recipeMaker"));
            if (gunPath2.Element("techLevel") != null) tl2 = new XElement(gunPath2.Element("techLevel"));
            if (ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='{baseWeapon}']/statBases/WorkToMake") != null) wtm2 = new XElement(ElementAtPath(defCopy.Root, $"ThingDef[@ParentName='{baseWeapon}']/statBases/WorkToMake"));

            if (!(sender as CheckBox).Checked)
            {
                // Probably should not just delete everything when unchecking, right?

                if (cl != null) cl.Remove();
                if (rm != null) rm.Remove();
                if (tl != null) tl.Remove();
                if (wtm != null) wtm.Remove();
            }
            else
            {
                // Paste
                if (cl2 != null) gunPath.Add(new XElement(cl2));
                if (rm2 != null) gunPath.Add(new XElement(rm2));
                if (tl2 != null) gunPath.Add(new XElement(tl2));
                if (wtm2 != null) gunPath.Add(new XElement(wtm2));
            }
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
            if (!elem.Nodes().Any()) elem.Remove();
        }

        private void CheckSwitch(object sender, EventArgs e)
        {
            var cb = (CheckBox) sender;
            var controlName = cb.Name;

            var li = (dynamic)null; // oh i see
            var li2 = (dynamic)null;
            string partName;

            if (!(sender as CheckBox).Checked)
                defCopy = new(XDocument.Parse(def.ToString()));

            if (controlName is not ("chb_isBurst" or "chb_isIncendiary")) CheckTools();

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
                        if (bc != null) bc.Remove();
                        if (bd != null) bd.Remove();
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

                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools") != null)
                    {
                        if (!ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools")
                                .Add(new XElement("li",
                                        new XElement("label", partName),
                                        new XElement("capacities", new XElement("li", "Blunt"),
                                                                   new XElement("li", "Poke")),
                                        new XElement("power", nud_barrelDamage.Value),
                                        new XElement("cooldownTime", nud_barrelCooldown.Value)
                                        )
                                );
                        }
                    }

                    #region -= Hide/Show =-


                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools/li") != null)
                    {
                        li = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    }

                    // Copy
                    if (li != null) li2 = new XElement(ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First());

                    if (!(sender as CheckBox).Checked)
                    {   // "Hide"
                        if (li != null) li.Remove();
                        CheckTools();
                    }
                    else
                    {   // Paste
                        if (li2 != null && li == null) gunPath.Element("tools").Add(new XElement(li2));
                    }
                    #endregion

                    UpdateString();

                    break;
                case "chb_hasStock": //TODO: test and finish this thing
                    nud_stockDamage.Enabled = nud_stockCooldown.Enabled = l_m3.Enabled = l_m4.Enabled = cb.Checked;

                    partName = "stock";

                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools") != null)
                    {
                        if (!ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools")
                                .Add(new XElement("li",
                                        new XElement("label", partName),
                                        new XElement("capacities", new XElement("li", "Blunt")),
                                        new XElement("power", nud_stockDamage.Value),
                                        new XElement("cooldownTime", nud_stockCooldown.Value)
                                        )
                                );
                        }
                    }

                    #region -= Hide/Show =-


                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools/li") != null)
                    {
                        li = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    }

                    // Copy
                    if (li != null) li2 = new XElement(ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First());

                    if (!(sender as CheckBox).Checked)
                    {   // "Hide"
                        if (li != null) li.Remove();
                        CheckTools();
                    }
                    else
                    {   // Paste
                        if (li2 != null && li == null) gunPath.Element("tools").Add(new XElement(li2));
                    }
                    #endregion

                    UpdateString();

                    break;

                case "chb_hasGrip":
                    nud_gripDamage.Enabled = nud_gripCooldown.Enabled = l_m5.Enabled = l_m6.Enabled = cb.Checked;

                    partName = "grip";

                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools") != null)
                    {
                        if (!ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools")
                                .Add(new XElement("li",
                                        new XElement("label", partName),
                                        new XElement("capacities", new XElement("li", "Blunt")),
                                        new XElement("power", nud_gripDamage.Value),
                                        new XElement("cooldownTime", nud_gripCooldown.Value)
                                        )
                                );
                        }
                    }

                    #region -= Hide/Show =-


                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools/li") != null)
                    {
                        li = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    }

                    // Copy
                    if (li != null) li2 = new XElement(ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First());

                    if (!(sender as CheckBox).Checked)
                    {   // "Hide"
                        if (li != null) li.Remove();
                        CheckTools();
                    }
                    else
                    {   // Paste
                        if (li2 != null && li == null) gunPath.Element("tools").Add(new XElement(li2));
                    }
                    #endregion

                    UpdateString(); 
                    
                    break;
                case "chb_hasBlade":
                    nud_bladeDamage.Enabled = nud_bladeCooldown.Enabled = l_m7.Enabled = l_m8.Enabled = cb.Checked;

                    partName = "blade";

                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools") != null)
                    {
                        if (!ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools")
                                .Add(new XElement("li",
                                        new XElement("label", partName),
                                        new XElement("capacities", new XElement("li", "Cut"),
                                                                   new XElement("li", "Stab")),
                                        new XElement("power", nud_bladeDamage.Value),
                                        new XElement("cooldownTime", nud_bladeCooldown.Value)
                                        )
                                );
                        }
                    }

                    #region -= Hide/Show =-


                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools/li") != null)
                    {
                        li = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    }

                    // Copy
                    if (li != null) li2 = new XElement(ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First());

                    if (!(sender as CheckBox).Checked)
                    {   // "Hide"
                        if (li != null) li.Remove();
                        CheckTools();
                    }
                    else
                    {   // Paste
                        if (li2 != null && li == null) gunPath.Element("tools").Add(new XElement(li2));
                    }
                    #endregion

                    UpdateString(); 
                    break;
                case "chb_hasLimb":
                    nud_limbDamage.Enabled = nud_limbCooldown.Enabled = l_m9.Enabled = l_m10.Enabled = cb.Checked;

                    partName = "limb";

                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools") != null)
                    {
                        if (!ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).Any())
                        {
                            ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools")
                                .Add(new XElement("li",
                                        new XElement("label", partName),
                                        new XElement("capacities", new XElement("li", "Blunt"),
                                                                   new XElement("li", "Poke")),
                                        new XElement("power", nud_limbDamage.Value),
                                        new XElement("cooldownTime", nud_limbCooldown.Value)
                                        )
                                );
                        }
                    }

                    #region -= Hide/Show =-


                    if (ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools/li") != null)
                    {
                        li = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    }

                    // Copy
                    if (li != null) li2 = new XElement(ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First());

                    if (!(sender as CheckBox).Checked)
                    {   // "Hide"
                        if (li != null) li.Remove();
                        CheckTools();
                    }
                    else
                    {   // Paste
                        if (li2 != null && li == null) gunPath.Element("tools").Add(new XElement(li2));
                    }
                    #endregion

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

        private void MeleeDmgCD_Changed(object sender, EventArgs e)
        {
            var nud = (NumericUpDown)sender;
            var cn = nud.Name;
            var value = nud.Value.ToString();

            string partName;
            XElement parentElem;

            switch (cn)
            {
                case "nud_barrelDamage" or "nud_barrelCooldown":
                    partName = "barrel";
                    parentElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    if (cn == "nud_barrelDamage") parentElem.Element("power").Value = value;
                    else parentElem.Element("cooldownTime").Value = value;
                    break;
                case "nud_stockDamage" or "nud_stockCooldown":
                    partName = "stock";
                    parentElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    if (cn == "nud_stockDamage") parentElem.Element("power").Value = value;
                    else parentElem.Element("cooldownTime").Value = value;
                    break;
                case "nud_gripDamage" or "nud_gripCooldown":
                    partName = "grip";
                    parentElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    if (cn == "nud_gripDamage") parentElem.Element("power").Value = value;
                    else parentElem.Element("cooldownTime").Value = value;
                    break;
                case "nud_bladeDamage" or "nud_bladeCooldown":
                    partName = "blade";
                    parentElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    if (cn == "nud_bladeDamage") parentElem.Element("power").Value = value;
                    else parentElem.Element("cooldownTime").Value = value;
                    break;
                case "nud_limbDamage" or "nud_limbCooldown":
                    partName = "limb";
                    parentElem = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/tools").Descendants("li").Where(p => p.Element("label")?.Value == partName).First();
                    if (cn == "nud_limbDamage") parentElem.Element("power").Value = value;
                    else parentElem.Element("cooldownTime").Value = value;
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
            if (string.IsNullOrEmpty(ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/defaultProjectile").Value))
                ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseWeapon}']/verbs/li/defaultProjectile").Value = ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/defName") == null ? "" : ElementAtPath(def.Root, $"ThingDef[@ParentName='{baseBullet}']/defName").Value;

            if (!elem.Nodes().Any()) elem.Remove();
        }

        private void VerbsItem_KeyPress(object sender, KeyPressEventArgs e)
        {
            CheckVerbs();
        }

        private void VerbsItem_Changed(object sender, EventArgs e)
        {
            CheckVerbs();

            var parentElem = gunPath.Element("verbs").Element("li");

            string tag;

            string controlName = ((Control)sender).Name;
            var value = sender is NumericUpDown nud ? nud.Value.ToString() : ((ComboBox)sender).SelectedItem.ToString();

            switch (controlName) // im surprised this works
            {
                case "nud_rangedWarmup":
                    tag = "warmupTime";
                    break;
                case "nud_range":
                    tag = "range";
                    break;
                case "nud_burstCount":
                    tag = "burstShotCount";
                    break;
                case "nud_burstDelay":
                    tag = "ticksBetweenBurstShots";
                    break;
                case "cb_shotSound":
                    tag = "soundCast";
                    break;
                case "cb_shotTailSound":
                    tag = "soundCastTail";
                    break;
                case "nud_muzzleflashScale":
                    tag = "muzzleFlashScale";
                    break;
                default:
                    return;
            }

            var elem = parentElem.Element(tag);

            if ((int.TryParse(value, out int i) && i == 0) || string.IsNullOrEmpty(value))
            {
                if (elem != null) elem.Remove();
                CheckVerbs(); // Check if <statBases> has no children
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

            if (!elem.Nodes().Any()) elem.Remove();
        }

        private void ProjectileItem_Changed(object sender, EventArgs e)
        {
            CheckProjectile();

            var parentElem = bulletPath.Element("projectile");

            string tag;

            string controlName = ((Control)sender).Name;
            var value = sender is NumericUpDown nud ? nud.Value.ToString() : ((ComboBox)sender).SelectedItem.ToString();

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
                    bool bomb = cb_damageDef.SelectedItem.ToString() == "Bomb";
                    l_bomb1.Enabled = l_bomb2.Enabled = l_bomb3.Enabled 
                        = nud_explRadius.Enabled = nud_explDelay.Enabled = nud_arcHeight.Enabled 
                        = chb_isIncendiary.Enabled = bomb;
                    if (!bomb) // brain fart?
                    {
                        if (parentElem.Element("ai_IsIncendiary") != null) 
                            parentElem.Element("ai_IsIncendiary").Remove();
                        if (parentElem.Element("explosionRadius") != null)
                            parentElem.Element("explosionRadius").Remove();
                        if (parentElem.Element("explosionDelay") != null)
                            parentElem.Element("explosionDelay").Remove();
                        if (parentElem.Element("arcHeightFactor") != null)
                            parentElem.Element("arcHeightFactor").Remove();
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
                if (elem != null) elem.Remove();
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

        static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}