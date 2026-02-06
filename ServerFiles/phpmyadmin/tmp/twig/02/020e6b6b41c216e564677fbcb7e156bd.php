<?php

use Twig\Environment;
use Twig\Error\LoaderError;
use Twig\Error\RuntimeError;
use Twig\Extension\CoreExtension;
use Twig\Extension\SandboxExtension;
use Twig\Markup;
use Twig\Sandbox\SecurityError;
use Twig\Sandbox\SecurityNotAllowedTagError;
use Twig\Sandbox\SecurityNotAllowedFilterError;
use Twig\Sandbox\SecurityNotAllowedFunctionError;
use Twig\Source;
use Twig\Template;

/* server/plugins/index.twig */
class __TwigTemplate_83c6873570a1a3e806c0baf900c3f850 extends Template
{
    private $source;
    private $macros = [];

    public function __construct(Environment $env)
    {
        parent::__construct($env);

        $this->source = $this->getSourceContext();

        $this->parent = false;

        $this->blocks = [
        ];
    }

    protected function doDisplay(array $context, array $blocks = [])
    {
        $macros = $this->macros;
        // line 1
        yield "<div class=\"container-fluid\">
<h2>
  ";
        // line 3
        yield PhpMyAdmin\Html\Generator::getImage("b_plugin");
        yield "
  ";
yield _gettext("Plugins");
        // line 5
        yield "</h2>

<div id=\"plugins_plugins\">
  <div class=\"card\">
    <div class=\"card-body\">
      ";
        // line 10
        $context['_parent'] = $context;
        $context['_seq'] = CoreExtension::ensureTraversable(Twig\Extension\CoreExtension::keys(($context["plugins"] ?? null)));
        foreach ($context['_seq'] as $context["_key"] => $context["type"]) {
            // line 11
            yield "        <a class=\"btn btn-primary\" href=\"#plugins-";
            yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape((($__internal_compile_0 = ($context["clean_types"] ?? null)) && is_array($__internal_compile_0) || $__internal_compile_0 instanceof ArrayAccess ? ($__internal_compile_0[$context["type"]] ?? null) : null), "html", null, true);
            yield "\">
          ";
            // line 12
            yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape($context["type"], "html", null, true);
            yield "
        </a>
      ";
        }
        $_parent = $context['_parent'];
        unset($context['_seq'], $context['_iterated'], $context['_key'], $context['type'], $context['_parent'], $context['loop']);
        $context = array_intersect_key($context, $_parent) + $_parent;
        // line 15
        yield "    </div>
  </div>
  ";
        // line 17
        $context['_parent'] = $context;
        $context['_seq'] = CoreExtension::ensureTraversable(($context["plugins"] ?? null));
        foreach ($context['_seq'] as $context["type"] => $context["list"]) {
            // line 18
            yield "    <div class=\"row\">
      <div class=\"table-responsive col-12\">
        <table class=\"table table-striped table-hover caption-top w-auto\" id=\"plugins-";
            // line 20
            yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape((($__internal_compile_1 = ($context["clean_types"] ?? null)) && is_array($__internal_compile_1) || $__internal_compile_1 instanceof ArrayAccess ? ($__internal_compile_1[$context["type"]] ?? null) : null), "html", null, true);
            yield "\">
          <caption>
            ";
            // line 22
            yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape($context["type"], "html", null, true);
            yield "
          </caption>
          <thead>
            <tr>
              <th scope=\"col\">";
yield _gettext("Plugin");
            // line 26
            yield "</th>
              <th scope=\"col\">";
yield _gettext("Description");
            // line 27
            yield "</th>
              <th scope=\"col\">";
yield _gettext("Version");
            // line 28
            yield "</th>
              <th scope=\"col\">";
yield _gettext("Author");
            // line 29
            yield "</th>
              <th scope=\"col\">";
yield _gettext("License");
            // line 30
            yield "</th>
            </tr>
          </thead>
          <tbody>
            ";
            // line 34
            $context['_parent'] = $context;
            $context['_seq'] = CoreExtension::ensureTraversable($context["list"]);
            foreach ($context['_seq'] as $context["_key"] => $context["plugin"]) {
                // line 35
                yield "              <tr class=\"noclick\">
                <th>
                  ";
                // line 37
                yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape(CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "name", [], "any", false, false, false, 37), "html", null, true);
                yield "
                  ";
                // line 38
                if ((CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "status", [], "any", false, false, false, 38) != "ACTIVE")) {
                    // line 39
                    yield "                    <span class=\"badge bg-danger\">
                      ";
                    // line 40
                    if ((CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "status", [], "any", false, false, false, 40) == "INACTIVE")) {
                        // line 41
                        yield "                        ";
yield _gettext("inactive");
                        // line 42
                        yield "                      ";
                    } elseif ((CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "status", [], "any", false, false, false, 42) == "DISABLED")) {
                        // line 43
                        yield "                        ";
yield _gettext("disabled");
                        // line 44
                        yield "                      ";
                    } elseif ((CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "status", [], "any", false, false, false, 44) == "DELETING")) {
                        // line 45
                        yield "                        ";
yield _gettext("deleting");
                        // line 46
                        yield "                      ";
                    } elseif ((CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "status", [], "any", false, false, false, 46) == "DELETED")) {
                        // line 47
                        yield "                        ";
yield _gettext("deleted");
                        // line 48
                        yield "                      ";
                    }
                    // line 49
                    yield "                    </span>
                  ";
                }
                // line 51
                yield "                </th>
                <td>";
                // line 52
                yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape(CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "description", [], "any", false, false, false, 52), "html", null, true);
                yield "</td>
                <td>";
                // line 53
                yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape(CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "version", [], "any", false, false, false, 53), "html", null, true);
                yield "</td>
                <td>";
                // line 54
                yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape(CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "author", [], "any", false, false, false, 54), "html", null, true);
                yield "</td>
                <td>";
                // line 55
                yield $this->env->getRuntime('Twig\Runtime\EscaperRuntime')->escape(CoreExtension::getAttribute($this->env, $this->source, $context["plugin"], "license", [], "any", false, false, false, 55), "html", null, true);
                yield "</td>
              </tr>
            ";
            }
            $_parent = $context['_parent'];
            unset($context['_seq'], $context['_iterated'], $context['_key'], $context['plugin'], $context['_parent'], $context['loop']);
            $context = array_intersect_key($context, $_parent) + $_parent;
            // line 58
            yield "          </tbody>
        </table>
      </div>
    </div>
  ";
        }
        $_parent = $context['_parent'];
        unset($context['_seq'], $context['_iterated'], $context['type'], $context['list'], $context['_parent'], $context['loop']);
        $context = array_intersect_key($context, $_parent) + $_parent;
        // line 63
        yield "</div>
</div>
";
        return; yield '';
    }

    /**
     * @codeCoverageIgnore
     */
    public function getTemplateName()
    {
        return "server/plugins/index.twig";
    }

    /**
     * @codeCoverageIgnore
     */
    public function isTraitable()
    {
        return false;
    }

    /**
     * @codeCoverageIgnore
     */
    public function getDebugInfo()
    {
        return array (  200 => 63,  190 => 58,  181 => 55,  177 => 54,  173 => 53,  169 => 52,  166 => 51,  162 => 49,  159 => 48,  156 => 47,  153 => 46,  150 => 45,  147 => 44,  144 => 43,  141 => 42,  138 => 41,  136 => 40,  133 => 39,  131 => 38,  127 => 37,  123 => 35,  119 => 34,  113 => 30,  109 => 29,  105 => 28,  101 => 27,  97 => 26,  89 => 22,  84 => 20,  80 => 18,  76 => 17,  72 => 15,  63 => 12,  58 => 11,  54 => 10,  47 => 5,  42 => 3,  38 => 1,);
    }

    public function getSourceContext()
    {
        return new Source("", "server/plugins/index.twig", "C:\\Users\\admin\\source\\repos\\PAMP\\PAMP\\bin\\Debug\\net8.0-windows\\bin\\phpmyadmin\\templates\\server\\plugins\\index.twig");
    }
}
